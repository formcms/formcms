import { utcStrToDatetimeStr } from "./utils/formatter.js";
import {fetchPagePart, initIntersectionObserver} from "./utils/datalist.js";

document.querySelectorAll('[data-component="previous"]').forEach(el => {
    el.addEventListener('click', e => handlePaginationButton(e, e.target, false));
});

document.querySelectorAll('[data-component="next"]').forEach(el => {
    el.addEventListener('click', e => handlePaginationButton(e, e.target, true));
});

formatDate();
initIntersectionObserver();
updatePaginationStatus();

function formatDate() {
    document.querySelectorAll('[data-component="localDateTime"]').forEach(el => {
        const isoDate = el.textContent.trim();
        if (isoDate) {
            const formattedDate = utcStrToDatetimeStr(isoDate);
            el.textContent = formattedDate || isoDate;
        }
    });
}

function updatePaginationStatus() {
    document.querySelectorAll('[data-component="data-list"]').forEach(list => {
        const pagination = list.getAttribute('pagination');
        const each = list.querySelector('[data-component="foreach"]');
        const first = each?.getAttribute('first');
        const last = each?.getAttribute('last');
        const nav = list.querySelector(':has([data-component="previous"])'); 

        if (!nav) return;

        if (pagination !== 'Button' || (!first && !last)) {
            nav.remove();
            return;
        }

        toggleButtonVisibility(nav.querySelector('[data-component="previous"]'), !!first);
        toggleButtonVisibility(nav.querySelector('[data-component="next"]'), !!last);
    });
}

function toggleButtonVisibility(button, isVisible) {
    if (!button) return;
    button.style.display = isVisible ? '' : 'none';
}

async function handlePaginationButton(event, button, isNext) {
    event.preventDefault();
    const container = button.closest('[data-component="data-list"]');
    const list = container.querySelector('[data-component="foreach"]');
    const token = list.getAttribute(isNext ? "last" : "first");
    const sourceId = list.getAttribute("source_id");
    const response = await fetchPagePart(sourceId, token, true);
    if (response) {
        list.outerHTML = response;
        updatePaginationStatus();
    }
}