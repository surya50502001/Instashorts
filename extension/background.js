// Scroll Guardian - Background Service Worker

const DEFAULT_API_URL = 'http://localhost:5000/api';
let eventQueue = [];
let isProcessingQueue = false;

// Initialize on startup
chrome.runtime.onInstalled.addListener(async () => {
  console.log('[Scroll Guardian] Extension installed.');
  const data = await chrome.storage.local.get(['apiUrl', 'trackingEnabled', 'interventionEnabled', 'authToken']);
  if (!data.apiUrl) await chrome.storage.local.set({ apiUrl: DEFAULT_API_URL });
  if (data.trackingEnabled === undefined) await chrome.storage.local.set({ trackingEnabled: true });
  if (data.interventionEnabled === undefined) await chrome.storage.local.set({ interventionEnabled: true });
  
  // Setup heartbeat alarm every 1 minute
  chrome.alarms.create('scrollguardian_heartbeat', { periodInMinutes: 1 });
});

chrome.alarms.onAlarm.addListener(async (alarm) => {
  if (alarm.name === 'scrollguardian_heartbeat') {
    await processHeartbeat();
    await processEventQueue();
  }
});

// Listen for messages from content script & popup
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  handleMessage(message, sender).then(sendResponse).catch(err => {
    console.error('[Scroll Guardian] Message handling error:', err);
    sendResponse({ success: false, error: err.message });
  });
  return true; // async response
});

async function handleMessage(message, sender) {
  const { action, payload } = message;

  switch (action) {
    case 'INGEST_EVENT': {
      const storage = await chrome.storage.local.get(['authToken', 'apiUrl', 'trackingEnabled', 'sessionId']);
      if (!storage.trackingEnabled) return { success: true, ignored: true };

      const eventPayload = {
        ...payload,
        sessionId: storage.sessionId || null,
        deviceIdentifier: 'browser_extension_chrome'
      };

      if (!storage.authToken) {
        // Queue locally until logged in
        eventQueue.push(eventPayload);
        await chrome.storage.local.set({ pendingEvents: eventQueue });
        return { success: true, queued: true, unauthenticated: true };
      }

      try {
        const res = await fetch(`${storage.apiUrl || DEFAULT_API_URL}/content/events`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${storage.authToken}`
          },
          body: JSON.stringify(eventPayload)
        });

        if (res.ok) {
          const result = await res.json();
          if (result.sessionId && result.sessionId !== storage.sessionId) {
            await chrome.storage.local.set({ sessionId: result.sessionId });
          }
          return { success: true, data: result };
        } else {
          // Add to retry queue
          eventQueue.push(eventPayload);
          await chrome.storage.local.set({ pendingEvents: eventQueue });
          return { success: false, queued: true, status: res.status };
        }
      } catch (err) {
        eventQueue.push(eventPayload);
        await chrome.storage.local.set({ pendingEvents: eventQueue });
        return { success: false, queued: true, error: err.message };
      }
    }

    case 'SYNC_TOKEN': {
      await chrome.storage.local.set({
        authToken: payload.token,
        userEmail: payload.email,
        userName: payload.name
      });
      await processEventQueue();
      return { success: true };
    }

    case 'GET_STATUS': {
      const storage = await chrome.storage.local.get([
        'authToken', 'apiUrl', 'trackingEnabled', 'interventionEnabled', 'sessionId', 'userEmail', 'userName'
      ]);
      return {
        success: true,
        isAuthenticated: !!storage.authToken,
        trackingEnabled: storage.trackingEnabled !== false,
        interventionEnabled: storage.interventionEnabled !== false,
        userEmail: storage.userEmail,
        userName: storage.userName,
        sessionId: storage.sessionId
      };
    }

    case 'INTERVENTION_ACTION': {
      const storage = await chrome.storage.local.get(['authToken', 'apiUrl']);
      if (!storage.authToken) return { success: false, error: 'Unauthenticated' };

      const res = await fetch(`${storage.apiUrl || DEFAULT_API_URL}/interventions/action`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${storage.authToken}`
        },
        body: JSON.stringify({
          interventionId: payload.interventionId,
          response: payload.response
        })
      });
      return { success: res.ok };
    }

    default:
      return { success: false, error: `Unknown action: ${action}` };
  }
}

async function processHeartbeat() {
  const storage = await chrome.storage.local.get(['authToken', 'apiUrl', 'sessionId', 'trackingEnabled']);
  if (!storage.authToken || !storage.trackingEnabled) return;

  try {
    const res = await fetch(`${storage.apiUrl || DEFAULT_API_URL}/content/session/heartbeat`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${storage.authToken}`
      },
      body: JSON.stringify({
        sessionId: storage.sessionId || null,
        deviceIdentifier: 'browser_extension_chrome',
        activeScrollSecondsIncrement: 30,
        idleSecondsIncrement: 30
      })
    });

    if (res.ok) {
      const heartbeat = await res.json();
      if (heartbeat.sessionId && heartbeat.sessionId !== storage.sessionId) {
        await chrome.storage.local.set({ sessionId: heartbeat.sessionId });
      }

      if (heartbeat.triggerIntervention && heartbeat.intervention) {
        // Broadcast intervention to active tabs
        const tabs = await chrome.tabs.query({ active: true, currentWindow: true });
        for (const tab of tabs) {
          if (tab.id) {
            chrome.tabs.sendMessage(tab.id, {
              action: 'SHOW_INTERVENTION',
              intervention: heartbeat.intervention
            }).catch(() => {});
          }
        }
      }
    }
  } catch (err) {
    console.warn('[Scroll Guardian] Heartbeat failed:', err.message);
  }
}

async function processEventQueue() {
  if (isProcessingQueue) return;
  isProcessingQueue = true;

  try {
    const storage = await chrome.storage.local.get(['authToken', 'apiUrl', 'pendingEvents']);
    if (!storage.authToken || !storage.pendingEvents || storage.pendingEvents.length === 0) {
      isProcessingQueue = false;
      return;
    }

    const pending = [...storage.pendingEvents];
    const remaining = [];

    for (const ev of pending) {
      try {
        const res = await fetch(`${storage.apiUrl || DEFAULT_API_URL}/content/events`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${storage.authToken}`
          },
          body: JSON.stringify(ev)
        });

        if (!res.ok && res.status >= 500) {
          remaining.push(ev);
        }
      } catch {
        remaining.push(ev);
      }
    }

    await chrome.storage.local.set({ pendingEvents: remaining });
    eventQueue = remaining;
  } finally {
    isProcessingQueue = false;
  }
}
