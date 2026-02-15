const apiPrefix = "/api";

export async function postData(url, data) {
    const response =await fetch(apiPrefix + url, {
        body: JSON.stringify(data),
        method: 'POST',
        credentials: 'include',
        headers: {
            'Content-Type': 'application/json',
        },
    });
    return await parseResponse(response);
}

export async function getJson(url) {
    const res = await fetch(apiPrefix + url);
    return await parseResponse(res);
}

async function parseResponse(response) {
    if (!response.ok) {
        return {error:`HTTP error! Status: ${response.status}`};
    }else {
        const text = await response.text();
        return text ? JSON.parse(text):  null;
    }
}