

$(document).ready(function () {
    function acceptCookies() {
        document.cookie = "cookies-consent=true; path=/; max-age=" + (60 * 60 * 24 * 365);
        $('#cookie-consent-banner').remove();
    }

    function hasConsent() {
        return document.cookie.split(';').some(c => c.trim().startsWith('cookies-consent=true'));
    }
    if (!hasConsent()) {
        const banner = `
                <div id="cookie-consent-banner"
                     style="position:fixed; bottom:0; background:white; width:100%; padding:10px; text-align:center; box-shadow:0 -2px 10px rgba(0,0,0,0.2); z-index:9999;">
                    We use cookies to improve your experience.
                    <button id="accept-cookies-btn" class="btn btn-primary btn-sm">Accept</button>
                </div>`;
        $('body').append(banner);
        $('#accept-cookies-btn').on('click', acceptCookies);
    }
});
        
        