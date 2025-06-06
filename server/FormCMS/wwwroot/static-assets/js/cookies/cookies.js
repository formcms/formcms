loadBanner();

function acceptCookies() {
    document.cookie = "cookies-consent=true; path=/; max-age=" + (60 * 60 * 24 * 365);
    const banner = document.getElementById('cookie-consent-banner');
    if (banner) {
        banner.remove();
    }
}

function hasConsent() {
    return document.cookie.split(';').some(c => c.trim().startsWith('cookies-consent=true'));
}

function loadBanner() {

    if (!hasConsent()) {
        const banner = document.createElement('div');
        banner.id = 'cookie-consent-banner';
        banner.style.cssText = 'position:fixed; bottom:0; background:white; width:100%; padding:10px; text-align:center; box-shadow:0 -2px 10px rgba(0,0,0,0.2); z-index:9999;';
        banner.innerHTML = `
            We use cookies to improve your experience.
            <button id="accept-cookies-btn" class="btn btn-primary btn-sm">Accept</button>
        `;
        document.body.appendChild(banner);

        const button = document.getElementById('accept-cookies-btn');
        if (button) {
            button.addEventListener('click', acceptCookies);
        }
    }
}
