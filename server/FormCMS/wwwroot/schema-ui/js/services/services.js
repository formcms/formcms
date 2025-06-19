const apiPrefix = "/api";

async function tryFetch(cb) {
    const res = await cb();
    const text = await res.text();
    const  data = text ? JSON.parse(text):  null;
    return res.ok ? {data} : {error: (data?.title ??"An error has occurred. Please try again.")};
}


function buildFetch(url, options = {}) {
    return fetch(apiPrefix + url, {
        ...options,
        credentials: 'include',
        headers: {
            'Content-Type': 'application/json',
            ...(options.headers || {}),
        },
    });
}

export async function getUserInfo() {
    return await tryFetch(() => buildFetch(`/me`));
}

export async function logout() {
    return await tryFetch(() => buildFetch(`/logout`));
}

export async function list(type) {
    return await tryFetch(() => buildFetch(`/schemas?type=${type ?? ''}`));
}

export async function singleByName(name, type) {
    const url = `/schemas/name/${name}?type=${type}`;
    return await tryFetch(() => buildFetch(url));
}

export async function getHistory(schemaId) {
    return await tryFetch(() => buildFetch(`/schemas/history/${schemaId}`));
}

export async function single(id) {
    return await tryFetch(() => buildFetch(`/schemas/${id}`));
}

export async function save(data, publish) {
    let url = `/schemas`;
    if (publish) {
        url += '?publish=true';
    }
    return await tryFetch(() =>
        buildFetch(url, {
            method: 'POST',
            body: JSON.stringify(data),
        })
    );
}

export async function saveDefine(data) {
    return await tryFetch(() =>
        buildFetch(`/schemas/entity/define`, {
            method: 'POST',
            body: JSON.stringify(data),
        })
    );
}

export async function publish(data) {
    return await tryFetch(() =>
        buildFetch(`/schemas/publish`, {
            method: 'POST',
            body: JSON.stringify(data),
        })
    );
}

export async function define(name) {
    return await tryFetch(() => buildFetch(`/schemas/entity/${name}/define`));
}

export async function del(id) {
    return await tryFetch(() =>
        buildFetch(`/schemas/${id}`, {
            method: 'DELETE',
        })
    );
}
