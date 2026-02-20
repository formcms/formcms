const overlayId = 'custom-loading-overlay';
export function showOverlay() {
    injectStylesAndHtml();
    document.getElementById(overlayId).classList.remove('hidden');
}

export function hideOverlay() {
    const overlay = document.getElementById(overlayId);
    if (overlay) {
        overlay.classList.add('hidden');
    }
}

function injectStylesAndHtml() {
    // Check if overlay already exists
    if (document.getElementById(overlayId)) return;

    // Create overlay element
    const overlay = document.createElement('div');
    overlay.id = overlayId;
    overlay.className = 'overlay hidden';
    overlay.innerHTML = `
    <div class="spinner"></div>
  `;
    document.body.appendChild(overlay);

    // Create style element
    const style = document.createElement('style');
    style.innerHTML = `
    .overlay {
      position: fixed;
      top: 0; left: 0;
      width: 100%; height: 100%;
      background: rgba(0,0,0,0.5);
      display: flex; justify-content: center; align-items: center;
      z-index: 9999;
    }
    .overlay.hidden {
      display: none;
    }
    .spinner {
      border: 8px solid #f3f3f3;
      border-top: 8px solid #3498db;
      border-radius: 50%;
      width: 60px; height: 60px;
      animation: spin 1s linear infinite;
    }
    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }
  `;
    document.head.appendChild(style);
}


