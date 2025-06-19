import {getJson, postData} from "./util.js";

export async function saveBookmark(entity, id, payload) {
    return await postData(`/bookmarks/${entity}/${id}`, payload)
}

export async function fetchBookmarkFolders(entity, id) {
    return await getJson(`/bookmarks/folders/${entity}/${id}`)
}

export async function recordActivity(entity, id, type) {
    return await postData(`/activities/record/${entity}/${id}?type=${type}`)
}

export async function toggleActivity(entity, id, type, active) {
    return await postData(`/activities/toggle/${entity}/${id}?type=${type}&active=${active}`);
}

export async function fetchActivity(entity, id) {
    return await getJson(`/activities/${entity}/${id}`)
}
export async function trackVisit() {
    return await getJson(`/activities/visit?url=${encodeURIComponent(window.location.href)}`);
}
