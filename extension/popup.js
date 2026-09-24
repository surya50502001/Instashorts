document.addEventListener('DOMContentLoaded', async () => {
  const userDisplay = document.getElementById('user-display');
  const statusBadge = document.getElementById('status-badge');
  const toggleTracking = document.getElementById('toggle-tracking');
  const toggleInterventions = document.getElementById('toggle-interventions');
  const btnUseful = document.getElementById('btn-useful');
  const btnDashboard = document.getElementById('btn-dashboard');

  // Load status from background worker
  try {
    const status = await chrome.runtime.sendMessage({ action: 'GET_STATUS' });

    if (status.isAuthenticated) {
      userDisplay.textContent = status.userName || status.userEmail || 'Active User';
      statusBadge.textContent = 'Active';
      statusBadge.className = 'status-badge status-active';
    } else {
      userDisplay.textContent = 'Unauthenticated (Local Queue)';
      statusBadge.textContent = 'Offline';
      statusBadge.className = 'status-badge status-offline';
    }

    toggleTracking.checked = status.trackingEnabled;
    toggleInterventions.checked = status.interventionEnabled;
  } catch {
    userDisplay.textContent = 'Extension Active';
  }

  // Handle toggles
  toggleTracking.addEventListener('change', async (e) => {
    await chrome.storage.local.set({ trackingEnabled: e.target.checked });
  });

  toggleInterventions.addEventListener('change', async (e) => {
    await chrome.storage.local.set({ interventionEnabled: e.target.checked });
  });

  btnDashboard.addEventListener('click', () => {
    chrome.tabs.create({ url: 'http://localhost:5173' });
  });

  btnUseful.addEventListener('click', () => {
    chrome.tabs.create({ url: 'http://localhost:5173/recommendations' });
  });
});
