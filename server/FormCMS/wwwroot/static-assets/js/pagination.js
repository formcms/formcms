import {fetchPagePart, utcStrToDatetimeStr, initIntersectionObserver} from "./utils.js";
$(document).ready(function () {

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
        const sourceId = list.attributes["source_id"].value;
        const response = await fetchPagePart(sourceId,token, true);
        if (response) {
            list.outerHTML = response;
            updatePaginationStatus();
        }
    }
});
