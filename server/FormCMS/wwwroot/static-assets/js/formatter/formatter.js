import {utcStrToDatetimeStr} from "../utils/formatter.js";

document.querySelectorAll('[data-component="localDateTime"]').forEach(el => {
    const isoDate = el.textContent.trim();
    if (isoDate) {
        const formattedDate = utcStrToDatetimeStr(isoDate);
        el.textContent = formattedDate || isoDate;
    }
});