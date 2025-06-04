let user; 
let currentUserPromise;

export function getUser() {
    if (user) {
        return user;
    }

    const proceed = confirm("You must log in to perform this action. Do you want to log in now?");
    if (proceed) {
        window.location.href = "/portal?ref=" + encodeURIComponent(window.location.href);
    }
    return false;
}

export async function fetchUser() {
    if (user) return;
    if (currentUserPromise) {
        return currentUserPromise;
    }

    try {
        currentUserPromise = (async () => {
            const response = await fetch('/api/me', {
                credentials: 'include' // ensures cookies are sent with the request
            });

            if (response.ok) {
                user = await response.json();
                return user;
            }else {
                throw new Error('API call failed');
            }
        })();

        const result = await currentUserPromise;
        return result;
    } catch (error) {
        console.error('API call failed:', error);
        return false;
    } finally {
        // Clear the promise after completion
        currentUserPromise = null;
    }
}