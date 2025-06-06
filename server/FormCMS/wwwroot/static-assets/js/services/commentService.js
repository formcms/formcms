import {postData} from "./util.js";

export async function saveComments(entityName, recordId, content) {
    return  await postData("/comments", {entityName, recordId, content});
}

export async function deleteComments(id) {
    return  await postData(`/comments/delete/${id}`);
}

export async function updateComments(id, content) {
    return  await postData(`/comments/update`, {id, content});
}

export async function replyComments(referencedId, content) {
    return  await postData(`/comments/reply/${referencedId}`, {content});
}