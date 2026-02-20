// Import video.js module
import videojs from 'https://esm.sh/video.js@8.22.0';

// Init function
export function initPlayer(element) {
    element.querySelectorAll('.video-js').forEach(el => {
        const src = el.getAttribute('src');
        const player = videojs(el);
        player.src({
            src,
            type: 'application/x-mpegURL'
        });
    });
}
