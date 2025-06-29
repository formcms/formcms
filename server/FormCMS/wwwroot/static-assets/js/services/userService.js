export async function fetchMe() {
    const response = await fetch('/api/me', {
        credentials: 'include' // ensures cookies are sent with the request
    });

    if (response.ok) {
        return await response.json();
    }else {
        throw new Error('API call failed');
    }
}