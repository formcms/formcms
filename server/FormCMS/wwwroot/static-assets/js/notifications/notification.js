import {fetchUser, getUser} from "../utils/user.js";
import {getNotificationCount} from "../services/notificationService.js";

export function renderNotifications() {
    document.querySelectorAll('[data-component="notification-button"]').forEach(loadNotificationCount);
}

async function loadNotificationCount(button) {
    button.addEventListener('click', (e) => {
        window.location.href = '/portal?page=notifications';
    })
    
    await fetchUser();
    if (!getUser()) {
        button.style.display = 'none';
        console.log("not logged in");
        return;
    }
    try {
        const count = await getNotificationCount();
        const countElement = button.querySelector('[data-compoent="notification-count"]');
        if (countElement) {
            countElement.textContent = count;
            countElement.style.display = count > 0 ? 'flex' : 'none';
        }
    } catch (error) {
        console.error('Failed to load notification count:', error);
    }
}