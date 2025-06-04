import {utcStrToDatetimeStr} from "./formatter.js";

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
        // htmlContent might not be in a root element
        const contentDiv = document.createElement('div');
        contentDiv.innerHTML = htmlContent;

        // Process elements with data-component="localDateTime"
        const dateTimeElements = contentDiv.querySelectorAll('[data-component="localDateTime"]');
        dateTimeElements.forEach(element => {
            const isoDate = element.textContent.trim();
            if (isoDate) {
                const formattedDate = utcStrToDatetimeStr(isoDate); // Assumes utcStrToDatetimeStr is global
                element.textContent = formattedDate || isoDate;
            }
        });

        const formattedHtml = contentDiv.innerHTML;
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