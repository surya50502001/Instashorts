// Scroll Guardian - Content Script for Short-Form Content Platforms

(function () {
  'use strict';

  let currentUrl = location.href;
  let currentStartTime = Date.now();
  let activePlatform = detectPlatform(currentUrl);
  let hudElement = null;
  let activeInterventionModal = null;

  function detectPlatform(url) {
    if (url.includes('instagram.com/reel') || url.includes('instagram.com/reels')) return 'InstagramReels';
    if (url.includes('youtube.com/shorts')) return 'YouTubeShorts';
    if (url.includes('tiktok.com')) return 'TikTok';
    return null;
  }

  function init() {
    if (!activePlatform) return;
    console.log(`[Scroll Guardian] Active on ${activePlatform}: ${currentUrl}`);
    injectHud();
    startWatchTimer();
  }

  function injectHud() {
    if (document.getElementById('scroll-guardian-hud')) return;

    hudElement = document.createElement('div');
    hudElement.id = 'scroll-guardian-hud';
    hudElement.innerHTML = `
      <div class="sg-hud-pill">
        <div class="sg-hud-dot"></div>
        <span class="sg-hud-title">Scroll Guardian Active</span>
        <button id="sg-hud-useful-btn" class="sg-hud-action-btn">Find Something Useful</button>
      </div>
    `;
    document.body.appendChild(hudElement);

    document.getElementById('sg-hud-useful-btn')?.addEventListener('click', () => {
      fetchRecommendationAndShow();
    });
  }

  function startWatchTimer() {
    // Detect navigation in Single Page Apps (Instagram/YouTube/TikTok)
    let lastUrl = location.href;
    const observer = new MutationObserver(() => {
      if (location.href !== lastUrl) {
        onUrlChanged(lastUrl, location.href);
        lastUrl = location.href;
      }
    });
    observer.observe(document.body, { childList: true, subtree: true });

    window.addEventListener('popstate', () => {
      if (location.href !== lastUrl) {
        onUrlChanged(lastUrl, location.href);
        lastUrl = location.href;
      }
    });

    window.addEventListener('beforeunload', () => {
      sendCurrentVideoEvent();
    });
  }

  function onUrlChanged(oldUrl, newUrl) {
    sendCurrentVideoEvent();
    currentUrl = newUrl;
    currentStartTime = Date.now();
    activePlatform = detectPlatform(newUrl);
    if (activePlatform) injectHud();
  }

  function extractMetadata() {
    let title = '';
    let creator = '';
    let caption = '';

    if (activePlatform === 'YouTubeShorts') {
      const titleEl = document.querySelector('h2.title, yt-formatted-string.ytd-reel-player-header-renderer');
      const channelEl = document.querySelector('#channel-name, ytd-channel-name');
      title = titleEl?.innerText?.trim() || document.title;
      creator = channelEl?.innerText?.trim() || 'YouTube Creator';
    } else if (activePlatform === 'InstagramReels') {
      const authorEl = document.querySelector('a._a6hd, header a, div[role="button"] a');
      const captionEl = document.querySelector('h1._a9zc, div._a9zs, span._aade');
      creator = authorEl?.innerText?.trim() || 'Instagram Creator';
      caption = captionEl?.innerText?.trim() || '';
      title = caption ? (caption.length > 50 ? caption.substring(0, 47) + '...' : caption) : 'Instagram Reel';
    } else if (activePlatform === 'TikTok') {
      const descEl = document.querySelector('h1[data-e2e="browse-video-desc"], div.css-1694qba-DivContainer');
      const userEl = document.querySelector('span[data-e2e="browse-username"], h3[data-e2e="browse-username"]');
      caption = descEl?.innerText?.trim() || '';
      creator = userEl?.innerText?.trim() || 'TikTok Creator';
      title = caption || 'TikTok Video';
    }

    return { title, creator, caption };
  }

  function sendCurrentVideoEvent() {
    if (!activePlatform) return;
    const timeSpentSeconds = Math.max(1, Math.round((Date.now() - currentStartTime) / 1000));
    if (timeSpentSeconds < 3) return; // Ignore fleeting <3s skips

    const meta = extractMetadata();

    const payload = {
      url: currentUrl,
      sourceProvider: activePlatform === 'YouTubeShorts' ? 2 : (activePlatform === 'InstagramReels' ? 3 : 4),
      title: meta.title || 'Short-form Content',
      creator: meta.creator || 'Creator',
      caption: meta.caption || '',
      timeSpentSeconds: timeSpentSeconds,
      completionPercentage: Math.min(100, (timeSpentSeconds / 30) * 100),
      userAction: timeSpentSeconds >= 25 ? 3 : (timeSpentSeconds >= 10 ? 2 : 1)
    };

    chrome.runtime.sendMessage({
      action: 'INGEST_EVENT',
      payload: payload
    });
  }

  // Handle messages from background service worker (e.g. SHOW_INTERVENTION)
  chrome.runtime.onMessage.addListener((message) => {
    if (message.action === 'SHOW_INTERVENTION' && message.intervention) {
      showInterventionModal(message.intervention);
    }
  });

  function showInterventionModal(intervention) {
    if (activeInterventionModal) return;

    activeInterventionModal = document.createElement('div');
    activeInterventionModal.id = 'scroll-guardian-modal';
    activeInterventionModal.innerHTML = `
      <div class="sg-modal-backdrop">
        <div class="sg-modal-card">
          <div class="sg-modal-header">
            <span class="sg-badge">Scroll Guardian</span>
            <span class="sg-minutes">${intervention.sessionMinutes}m in this session</span>
          </div>
          <h2 class="sg-title">${escapeHtml(intervention.messageTitle)}</h2>
          <p class="sg-body">${escapeHtml(intervention.messageBody)}</p>
          
          <div class="sg-actions">
            <button id="sg-btn-useful" class="sg-primary-btn">
              ${escapeHtml(intervention.callToActionText || 'Find something useful')}
            </button>
            <button id="sg-btn-snooze" class="sg-secondary-btn">Snooze 15m</button>
            <button id="sg-btn-dismiss" class="sg-ghost-btn">Continue Scrolling</button>
          </div>
        </div>
      </div>
    `;

    document.body.appendChild(activeInterventionModal);

    document.getElementById('sg-btn-useful')?.addEventListener('click', () => {
      dismissModal();
      if (intervention.interventionId) {
        chrome.runtime.sendMessage({
          action: 'INTERVENTION_ACTION',
          payload: { interventionId: intervention.interventionId, response: 3 }
        });
      }
      if (intervention.attachedRecommendation) {
        showRecommendationDetail(intervention.attachedRecommendation);
      } else {
        fetchRecommendationAndShow();
      }
    });

    document.getElementById('sg-btn-snooze')?.addEventListener('click', () => {
      dismissModal();
      if (intervention.interventionId) {
        chrome.runtime.sendMessage({
          action: 'INTERVENTION_ACTION',
          payload: { interventionId: intervention.interventionId, response: 2 }
        });
      }
    });

    document.getElementById('sg-btn-dismiss')?.addEventListener('click', () => {
      dismissModal();
      if (intervention.interventionId) {
        chrome.runtime.sendMessage({
          action: 'INTERVENTION_ACTION',
          payload: { interventionId: intervention.interventionId, response: 1 }
        });
      }
    });
  }

  function dismissModal() {
    if (activeInterventionModal) {
      activeInterventionModal.remove();
      activeInterventionModal = null;
    }
  }

  async function fetchRecommendationAndShow() {
    dismissModal();
    const status = await chrome.runtime.sendMessage({ action: 'GET_STATUS' });
    const apiUrl = 'http://localhost:5000/api';
    const token = (await chrome.storage.local.get('authToken')).authToken;

    if (!token) {
      window.open('http://localhost:5173', '_blank');
      return;
    }

    try {
      const res = await fetch(`${apiUrl}/recommendations/find-useful`, {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      if (res.ok) {
        const rec = await res.json();
        if (rec.title) {
          showRecommendationDetail(rec);
        } else {
          window.open('http://localhost:5173/recommendations', '_blank');
        }
      }
    } catch {
      window.open('http://localhost:5173', '_blank');
    }
  }

  function showRecommendationDetail(rec) {
    dismissModal();
    activeInterventionModal = document.createElement('div');
    activeInterventionModal.id = 'scroll-guardian-modal';
    activeInterventionModal.innerHTML = `
      <div class="sg-modal-backdrop">
        <div class="sg-modal-card">
          <div class="sg-modal-header">
            <span class="sg-badge sg-badge-green">Recommended Next Step</span>
            <span class="sg-source">${escapeHtml(rec.sourceType || 'Curated')}</span>
          </div>
          <h2 class="sg-title">${escapeHtml(rec.title)}</h2>
          <p class="sg-body">${escapeHtml(rec.description)}</p>
          <div class="sg-reason-box">
            <strong>Why this recommendation:</strong>
            <p>${escapeHtml(rec.reasonDescription)}</p>
          </div>
          <div class="sg-actions">
            <a href="${escapeHtml(rec.url)}" target="_blank" id="sg-btn-open-rec" class="sg-primary-btn">
              Open Learning Resource ↗
            </a>
            <button id="sg-btn-close-rec" class="sg-secondary-btn">Close</button>
          </div>
        </div>
      </div>
    `;
    document.body.appendChild(activeInterventionModal);

    document.getElementById('sg-btn-close-rec')?.addEventListener('click', () => {
      dismissModal();
    });
  }

  function escapeHtml(str) {
    if (!str) return '';
    return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
  }

  // Initialize
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
