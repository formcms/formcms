const loadingDict = new Map();
export async function fetchPagePart(sourceId, token, replace) {
    if (!token || loadingDict.has(token)) {
        return; // Already loading
    }

    loadingDict.set(token, true);

    try {
        const url = new URL('/page_part', window.location.origin);
        url.searchParams.append('token', token);
        url.searchParams.append('replace', replace);
        if (sourceId) {
            url.searchParams.append('source', sourceId);
        }

        const response = await fetch(url.toString());

        if (!response.ok) throw new Error('Network response was not ok');

        let htmlContent = await response.text();
        //htmlContent might not be in a root element, 
        const $content = $(`<div>${htmlContent}</div>`);

        $content.find('[data-component="localDateTime"]').each(function() {
            const isoDate = $(this).text().trim();
            if (isoDate) {
                const formattedDate = utcStrToDatetimeStr(isoDate);
                $(this).text(formattedDate || isoDate);
            }
        });
        const formattedHtml = $content.prop('innerHTML');
        return formattedHtml;

    } catch (error) {
        console.error('Error loading more.', error);
    } finally {
        loadingDict.delete(token);
    }
}
export async function initIntersectionObserver() {
    const observer = new IntersectionObserver(async entries => {
        for (const entry of entries) {
            if (entry.isIntersecting) {
                const sourceId = entry.target.getAttribute("source_id");
                const token = entry.target.getAttribute('last');
                const response = await fetchPagePart(sourceId, token, false);
                if (response) {
                    const template = document.createElement('template');
                    template.innerHTML = response.trim();
                    entry.target.parentElement.appendChild(template.content);
                    entry.target.remove();
                    await initIntersectionObserver(); // Re-init for next load
                }
            }
        }
    });

    const trigger = document.querySelector(".load-more-trigger");
    if (trigger) observer.observe(trigger);
}
export function showToast(message) {
    $('.toast').remove();
    const $toast = $('<div class="toast"></div>').text(message);
    $('body').append($toast);
    setTimeout(() => $toast.addClass('show'), 10);
    setTimeout(() => {
        $toast.removeClass('show');
        setTimeout(() => $toast.remove(), 300);
    }, 2000);
}
// Store the ongoing API promise to prevent multiple calls
let currentUserPromise = null;

export async function currentUser() {
    // If an API call is already in progress, return its promise
    if (currentUserPromise) {
        return currentUserPromise;
    }

    try {
        // Store the new API promise
        currentUserPromise = (async () => {
            const response = await fetch('/api/me', {
                credentials: 'include' // ensures cookies are sent with the request
            });

            if (response.ok) {
                const userData = await response.json();
                // Store user data in localStorage
                const user = JSON.stringify(userData);
                localStorage.setItem('user', user);
                return user;
            }
            throw new Error('API call failed');
        })();

        const result = await currentUserPromise;
        return result;
    } catch (error) {
        console.error('API call failed:', error);
        return false;
    } finally {
        // Clear the promise after completion
        currentUserPromise = null;
    }
}
export function utcStrToDatetimeStr  (s)  {
    if (!s) return null
    const d = typeof(s) == 'string' ? utcStrToDatetime(s):s;
    return d.toLocaleDateString() + ' ' + d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

function utcStrToDatetime  (s)  {
    s = s.replaceAll(' ', 'T')
    if (!s.endsWith('Z')) {
        s += 'Z';
    }
    return new Date(s);
}