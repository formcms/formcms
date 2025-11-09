import {getJson, postData} from "./util.js";

export async function saveBookmark(entity, id, payload) {
    return await postData(`/bookmarks/${entity}/${id}`, payload)
}

export async function fetchBookmarkFolders(entity, id) {
    return await getJson(`/bookmarks/folders/${entity}/${id}`)
}

export async function fetchActivityStatus(entity, activityType, ids) {
    const param = ids.map(id=>`id=${id}`).join('&');
    const url = `/engagements/status/${entity}/${activityType}?${param}`;
    return await getJson(url);
}

export async function recordActivity(entity, id, type) {
    return await postData(`/engagements/mark/${entity}/${id}?type=${type}`)
}

export async function toggleActivity(entity, id, type, active) {
    return await postData(`/engagements/toggle/${entity}/${id}?type=${type}&active=${active}`);
}

export async function fetchActivity(entity, id) {
    return await getJson(`/engagements/${entity}/${id}`)
}
export async function trackVisit() {
    return await getJson(`/engagements/visit?url=${encodeURIComponent(window.location.href)}`);
}
