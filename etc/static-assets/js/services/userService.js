export async function fetchMe() {
    const response = await fetch('/api/me', {
        credentials: 'include'
    });
    if (response.ok) {
        return await response.json();
    }
}