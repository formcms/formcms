export const video = {
    category: 'Basic',
    name: 'video',
    label: 'Video',
    media: `
        <svg width="54" height="54" viewBox="0 0 54 54" fill="none" xmlns="http://www.w3.org/2000/svg">
            <rect x="4" y="4" width="46" height="46" rx="4" stroke="#446EB1" stroke-width="4" fill="none"/>
            <path d="M22 18L36 27L22 36V18Z" fill="#6D9EE8"/>
        </svg>
    `,
    content: `
<video class="video-js" controls preload="auto" data-setup='{"fluid": true, "controls": true, "fullscreen": {"options": {"navigationUI": "hide"}}}'></video>
    `,
};