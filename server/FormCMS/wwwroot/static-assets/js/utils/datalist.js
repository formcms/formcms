import {utcStrToDatetimeStr} from "./formatter.js";
import {getPart} from "../services/pageService.js";

const loadingDict = new Map();

export async function fetchPagePart(node,source,first,last) {
    console.log({first,last});
    const key = node + source??'' + first??'' + last??'' ;
    if (!node || loadingDict.has(key)) {
        return; // Already loading
    }
    loadingDict.set(key, true);
    try {
        const htmlContent = await getPart(node,source,first,last);
        const contentDiv = document.createElement('div');
        contentDiv.innerHTML = htmlContent;

        const dateTimeElements = contentDiv.querySelectorAll('[data-component="localDateTime"]');
        dateTimeElements.forEach(element => {
            const isoDate = element.textContent.trim();
            if (isoDate) {
                const formattedDate = utcStrToDatetimeStr(isoDate); // Assumes utcStrToDatetimeStr is global
                element.textContent = formattedDate || isoDate;
            }
        });
        return contentDiv.innerHTML;

    } catch (error) {
        console.error('Error loading more.', error);
    } finally {
        loadingDict.delete(key);
    }
}