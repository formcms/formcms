$(document).ready(function () {
    const loadingDict = new Map();

    $('[data-component="previous"]').click(e => handlePaginationButton(e, e.target, false));
    $('[data-component="next"]').click(e => handlePaginationButton(e, e.target, true));

    formatDate();
    initIntersectionObserver();
    updatePaginationStatus();

    function formatDate () {
        $('[data-component="localDateTime"]').each(function() {
            const isoDate = $(this).text().trim();
            if (isoDate) {
                const formattedDate = utcStrToDatetimeStr(isoDate);
                $(this).text(formattedDate || isoDate);
            }
        }); 
    }
    function updatePaginationStatus() {
        $('[data-component="data-list"]').each(function () {
            const $list = $(this);
            const pagination = $list.attr('pagination');
            
            const $each = $list.find('[data-component="foreach"]');
            const first = $each.attr('first');
            const last = $each.attr('last');
            const $nav = $list.find(':has([data-component="previous"])');
            
            console.log($nav.html());

            if (pagination !== 'Button' || (!first && !last)) {
                $nav.remove();
                return;
            }

            toggleButtonVisibility($nav.find('[data-component="previous"]'), !!first);
            toggleButtonVisibility($nav.find('[data-component="next"]'), !!last);
        });
    }

    function toggleButtonVisibility($button, isVisible) {
        isVisible ? $button.show() : $button.hide();
    }

    async function handlePaginationButton(event, button, isNext) {
        event.preventDefault();
        const container = button.parentElement.parentElement;
        const list = container.querySelector('[data-component="foreach"]');
        const token = list.attributes[isNext ? "last" : "first"].value;
        const response = await fetchPagePart(token);
        if (response) {
            list.outerHTML = response;
            updatePaginationStatus();
        }
    }
 
    async function fetchPagePart(token) {
        if (!token || loadingDict.has(token)) {
            return; // Already loading
        }

        loadingDict.set(token, true);

        try {
            const url = new URL('/page_part', window.location.origin);
            url.searchParams.append('token', token);

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
            console.log(htmlContent, formattedHtml);
            return formattedHtml;
            
        } catch (error) {
            console.error('Error loading more.', error);
        } finally {
            loadingDict.delete(token);
        }
    }

    function initIntersectionObserver() {
        const observer = new IntersectionObserver(async entries => {
            for (const entry of entries) {
                if (entry.isIntersecting) {
                    const token = entry.target.getAttribute('last');
                    const response = await fetchPagePart(token);
                    console.log(response);

                    if (response) {
                        const template = document.createElement('template');
                        template.innerHTML = response.trim();
                        entry.target.parentElement.appendChild(template.content);
                        entry.target.remove();
                        initIntersectionObserver(); // Re-init for next load
                    }
                }
            }
        });

        const trigger = document.querySelector(".load-more-trigger");
        if (trigger) observer.observe(trigger);
    }

    function utcStrToDatetimeStr  (s)  {
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
});
