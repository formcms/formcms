import {utcStrToDatetimeStr} from "../utils/formatter.js";

export function formatHtmlElement(element) {
    element.querySelectorAll('[data-component="localDateTime"]').forEach(el => {
        const isoDate = el.textContent.trim();
        if (isoDate) {
            const formattedDate = utcStrToDatetimeStr(isoDate);
            el.textContent = formattedDate || isoDate;
        }
    });
}