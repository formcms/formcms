import {fetchUser, getUser} from "../utils/user.js";
import {getNotificationCount} from "../services/notificationService.js";

export function renderNotifications() {
    document.querySelectorAll('[data-component="notification-button"]').forEach(loadNotificationCount);
}

async function loadNotificationCount(button) {
    button.addEventListener('click', (e) => {
        window.location.href = '/portal';
    })
    
    const user = await fetchUser();
    if (!user) {
        button.style.display = 'none';
        console.log("not logged in");
        return;
    }
    try {
        const count = await getNotificationCount();
        const countElement = button.querySelector('[data-component="notification-count"]');
        if (countElement) {
            countElement.textContent = count;
            countElement.style.display = count > 0 ? 'flex' : 'none';
        }else {
            console.error('Notification count could not be found.');
        }
    } catch (error) {
        console.error('Failed to load notification count:', error);
    }
}