import {fetchUser, getUser} from "../utils/user.js";

export function renderAvatar() {
    document.querySelectorAll('[data-component="user-button"]').forEach(loadNotificationCount);
}

async function loadNotificationCount(button) {
    const user = await fetchUser();
    if (!user) {
        button.style.display = 'none';
        return;
    }
    button.addEventListener('click', (e) => {
        window.location.href = '/portal';
    })
    // Remove existing SVG
    button.innerHTML = '';

    // Create and configure image element
    const img = document.createElement('img');
    img.src = user.avatarUrl;
    img.alt = 'User avatar';
    img.className = 'w-6 h-6 rounded-full object-cover';

    button.appendChild(img);
}