import {getUser} from "../utils/user.js";

export function renderNotifications() {
    document.querySelectorAll('[data-component="notification-button"]').forEach(loadNotificationCount);
}

async function loadNotificationCount(button) {
    if (!getUser()) {
        button.style.display = 'none';
        return;
    }
    try {
        
        const response = await fetch('/api/notifications/unread');
        if (!response.ok) throw new Error('Network response was not ok');

        const count = await response.json();

        const countElement = button.querySelector('[data-compoent="notification-count"]');
        if (countElement) {
            countElement.textContent = count;
            countElement.style.display = count > 0 ? 'flex' : 'none';
        }
    } catch (error) {
        console.error('Failed to load notification count:', error);
    }
}