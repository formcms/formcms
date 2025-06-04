import {apiPrefix} from "./util.js";

export async function saveComments(entityName, recordId, text) {
    const commentData = {
        EntityName: entityName,
        RecordId: recordId,
        Content: text,
    };
    const response = await fetch(`${apiPrefix}/comments`, {
        method: 'POST',
        body: JSON.stringify(commentData)
    });
    if (!response.ok) {
        return {error: `HTTP error! Status: ${response.status}`};
    }
}

export async function deleteComments(id) {
    const response = await fetch(`${apiPrefix}/comments/delete/${id}`, {
        method: 'POST',
    });
    if (!response.ok) {
        return {error: `HTTP error! Status: ${response.status}`};
    }
}

export async function updateComments(id, text) {
    const commentData = {
        id,
        Content: text,
    };
    const response = await fetch(`${apiPrefix}/comments/update`, {
        method: 'POST',
        body: JSON.stringify(commentData)
    });
    if (!response.ok) {
        return {error:`HTTP error! Status: ${response.status}`};
    }
}