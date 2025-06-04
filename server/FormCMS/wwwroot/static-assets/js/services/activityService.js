import {apiPrefix} from "./util.js";

export async function saveBookmark(entity, id, payload) {
    const res = await fetch(`${apiPrefix}/bookmarks/${entity}/${id}`, {
        method: 'POST',
        body: JSON.stringify(payload)
    });
    if (!res.ok) throw new Error('Failed to save bookmark');
}

export async function fetchBookmarkFolders(entity, id) {
    const res = await fetch(`${apiPrefix}/bookmarks/folders/${entity}/${id}`);
    return await res.json();
}

export async function recordActivity(entity, id, type) {
    await fetch(`${apiPrefix}/activities/record/${entity}/${id}?type=${type}`, {
        method: 'POST'
    });
}

export async function toggleActivity(entity, id, type, active) {
    const res = await fetch(`${apiPrefix}/activities/toggle/${entity}/${id}?type=${type}&active=${active}`, {
        method: 'POST'
    });
    return await res.json();
}

// API Functions
export async function fetchActivity(entity, id) {
    const res = await fetch(`${apiPrefix}/activities/${entity}/${id}`);
    return await res.json();
}
export async function trackVisit() {
    await fetch(`${apiPrefix}/activities/visit?url=${encodeURIComponent(window.location.href)}`);
}
