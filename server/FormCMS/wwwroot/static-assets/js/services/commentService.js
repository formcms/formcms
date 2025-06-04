
export async function saveComments(entityName, recordId, text) {
    const commentData = {
        EntityName: entityName,
        RecordId: recordId,
        Content: text,
    };
    const response = await fetch('/api/comments', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Accept': 'application/json'
        },
        body: JSON.stringify(commentData)
    });
    if (!response.ok) {
        throw new Error(`HTTP error! Status: ${response.status}`);
    }
}