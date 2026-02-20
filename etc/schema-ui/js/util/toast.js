export function showToast(message) {
    // Remove existing toast
    const existingToasts = document.querySelectorAll('.toast');
    existingToasts.forEach(t => t.remove());

    // Check if toast styles already exist
    const existingStyle = Array.from(document.head.querySelectorAll('style')).some(style =>
        style.textContent.includes('.toast {') && style.textContent.includes('.toast.show {')
    );

    // Create and append style element only if it doesn't exist
    if (!existingStyle) {
        const style = document.createElement('style');
        style.textContent = `
            .toast {
                position: fixed;
                bottom: 20px;
                left: 50%;
                transform: translateX(-50%);
                background-color: #4b5563; /* gray-600 */
                color: white;
                padding: 10px 20px;
                border-radius: 8px;
                box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
                z-index: 1000;
                opacity: 0;
                transition: opacity 0.3s ease-in-out;
            }
            .toast.show {
                opacity: 1;
            }
        `;
        document.head.appendChild(style);
    }

    // Create new toast
    const toast = document.createElement('div');
    toast.className = 'toast';
    toast.textContent = message;
    document.body.appendChild(toast);

    // Trigger show animation
    setTimeout(() => toast.classList.add('show'), 10);

    // Hide and remove after 2 seconds
    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    }, 2000);
}