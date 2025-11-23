export const shareDialogHTML = `
    <div id="share-dialog" class="modal modal-open">
        <div class="modal-box">
            <h3 class="font-bold text-lg">Share</h3>
            <div class="mt-4 flex flex-col gap-2">
                <button class="btn btn-outline share-option" data-platform="x">
                    <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
                        <path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z"/>
                    </svg>
                    Share to X
                </button>
                <button class="btn btn-outline share-option" data-platform="email">
                    <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
                        <path d="M20 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 4l-8 5-8-5V6l8 5 8-5v2z"/>
                    </svg>
                    Share via Email
                </button>
                <button class="btn btn-outline share-option" data-platform="reddit">
                    <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
                        <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm4.18 5.97c.57 0 1.03.46 1.03 1.03 0 .28-.11.53-.29.71-.37.37-.96.42-1.37.11-.15-.11-.25-.28-.29-.46-.05-.28-.02-.58.11-.83.18-.37.54-.56.81-.56zm-8.36 0c.27 0 .63.19.81.56.13.25.16.55.11.83-.04.18-.14.35-.29.46-.41.31-1 .26-1.37-.11-.18-.18-.29-.43-.29-.71 0-.57.46-1.03 1.03-1.03zm4.18 2.03c-1.38 0-2.5 1.12-2.5 2.5s1.12 2.5 2.5 2.5 2.5-1.12 2.5-2.5-1.12-2.5-2.5-2.5zm0 3.5c-.55 0-1-.45-1-1s.45-1 1-1 1 .45 1 1-.45 1-1 1zm4.15-1.47c-.18 0-.36.05-.52.15-.55.34-1.22.52-1.93.52s-1.38-.18-1.93-.52c-.16-.1-.34-.15-.52-.15-.55 0-1 .45-1 1 0 .27.11.52.29.71.55.58 1.38.96 2.36.96s1.81-.38 2.36-.96c.18-.19.29-.44.29-.71 0-.55-.45-1-1-1z"/>
                    </svg>
                    Share to Reddit
                </button>
                <button class="btn btn-outline share-option" data-platform="clipboard">
                    <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
                        <path d="M16 1H4c-1.1 0-2 .9-2 2v14h2V3h12V1zm3 4H8c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h11c1.1 0 2-.9 2-2V7c0-1.1-.9-2-2-2zm0 16H8V7h11v14z"/>
                    </svg>
                    Copy Link
                </button>
            </div>
            <div class="modal-action">
                <button id="cancel-share" class="btn">Cancel</button>
            </div>
        </div>
    </div>`;