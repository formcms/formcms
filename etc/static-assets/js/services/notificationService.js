import {getJson} from "./util.js";

export async function getNotificationCount() {
    return await getJson(`/notifications/unread`)
}
