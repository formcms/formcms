$(document).ready(function () {
    let loggedIn = false;
    isUserLoggedIn().then(x=> loggedIn = x);

    trackVisit();
    loadActivityBars();

    async function loadActivityBars() {
        $(`[data-component="activity-bar"]`).each(loadActivityBar);
    }

    async function loadActivityBar()
    {
        const activityBar = $(this);
        const entityName = activityBar.data('entity');
        const recordId = activityBar.data('record-id');

        const likeButton = activityBar.find('[data-component="like-button"]');
        const saveButton = activityBar.find('[data-component="save-button"]');
        const shareButton = activityBar.find('[data-component="share-button"]');
        const viewButton = activityBar.find('[data-component="view-button"]');

        
        try {
            const data = await fetchActivity(entityName, recordId);
            updateLikeButton(likeButton, data.like);
            updateViewButton(viewButton, data.view);
        } catch (err) {
            console.error('Error loading activity:', err);
        }

        likeButton.on('click', async function (e) {
            e.preventDefault();
            if (!loggedIn){
                window.location.href = "/portal?ref=" + encodeURIComponent(window.location.href);
                return;
            }

            const active = $(this).hasClass('active');
            try {
                const newCount = await toggleActivity(entityName, recordId, 'like', !active);
                updateLikeButton(likeButton, { count: newCount, active: !active });
            } catch (err) {
                console.error('Error toggling like:', err);
            }
        });

        // Replace the existing shareButton click handler in loadActivityBar
        shareButton.on('click', async function (e) {
            e.preventDefault();
            showShareDialog(entityName, recordId);
        });

        saveButton.on('click', () => showBookmarkModal(entityName, recordId));
    }
    // API Functions
    async function fetchActivity(entity, id) {
        const res = await fetch(`/api/activities/${entity}/${id}`);
        return await res.json();
    }
    async function trackVisit() {
        await fetch(`/api/activities/visit?url=${encodeURIComponent(window.location.href)}`);
    }

    async function toggleActivity(entity, id, type, active) {
        const res = await fetch(`/api/activities/toggle/${entity}/${id}?type=${type}&active=${active}`, {
            method: 'POST'
        });
        return await res.json();
    }

    async function recordActivity(entity, id, type) {
        await fetch(`/api/activities/record/${entity}/${id}?type=${type}`, {
            method: 'POST'
        });
    }

    async function fetchBookmarkFolders(entity, id) {
        const res = await fetch(`/api/bookmarks/folders/${entity}/${id}`);
        return await res.json();
    }

    async function saveBookmark(entity, id, payload) {
        const res = await fetch(`/api/bookmarks/${entity}/${id}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        if (!res.ok) throw new Error('Failed to save bookmark');
    }

    // UI Handlers
    function updateLikeButton($btn, data) {
        $btn.find('[data-component="like-count"]').text(formatCount(data.count));
        $btn.toggleClass('active', data.active);
        $btn.find('[data-component="like-icon-inactive"]').toggleClass('hidden', data.active);
        $btn.find('[data-component="like-icon-active"]').toggleClass('hidden', !data.active);
    }

    function updateViewButton($btn, data) {
        $btn.find('[data-component="view-count"]').text(formatCount(data.count));
    }

    async function showBookmarkModal(entityName, recordId) {
        if (!loggedIn){
            window.location.href = "/portal?ref=" + encodeURIComponent(window.location.href);
            return;
        }
        
        const $modal = $(`
            <div id="bookmark-modal" class="modal modal-open">
                <div class="modal-box">
                    <h3 class="font-bold text-lg">Save to Bookmark Folder</h3>
                    <div id="folder-list" class="mt-4 max-h-60 overflow-y-auto"></div>
                    <div class="mt-4">
                        <div class="form-control">
                            <label class="label">
                                <span class="label-text">Create New Folder</span>
                            </label>
                            <div class="flex space-x-2">
                                <input id="new-folder-name" type="text" placeholder="Enter folder name" class="input input-bordered w-full" />
                            </div>
                        </div>
                    </div>
                    <div class="modal-action">
                        <button id="save-bookmark" class="btn btn-primary">Save</button>
                        <button id="cancel-bookmark" class="btn">Cancel</button>
                    </div>
                </div>
            </div>
        `);
        $('body').append($modal);

        try {
            const folders = await fetchBookmarkFolders(entityName, recordId);
            const $folderList = $('#folder-list');
            folders.forEach(folder => {
                const $checkbox = $(`
                    <div class="form-control">
                        <label class="label cursor-pointer">
                            <span class="label-text">${folder.name || 'Default Folder'}</span>
                            <input type="checkbox" class="checkbox" data-folder-id="${folder.id}" ${folder.selected ? 'checked' : ''} />
                        </label>
                    </div>
                `);
                $folderList.append($checkbox);
            });
        } catch (err) {
            showToast('Failed to load bookmark folders');
            $modal.remove();
            return;
        }

        $('#save-bookmark').on('click', async function () {
            const selectedFolders = $('#folder-list input:checked').map(function () {
                return parseInt($(this).data('folder-id'));
            }).get();
            const newFolderName = $('#new-folder-name').val();

            try {
                await saveBookmark(entityName, recordId, { selectedFolders, newFolderName });
                showToast('Bookmarked successfully, you can see bookmarked items in User Portal');
                $modal.remove();
            } catch (err) {
                showToast('Failed to save bookmark');
            }
        });

        $('#cancel-bookmark').on('click', () => $modal.remove());
    }

    function formatCount(count) {
        if (count >= 1000) {
            const val = (count / 1000).toFixed(1);
            return val.endsWith('.0') ? `${val.slice(0, -2)}k` : `${val}k`;
        }
        return count.toString();
    }

    function showToast(message) {
        $('.toast').remove();
        const $toast = $('<div class="toast"></div>').text(message);
        $('body').append($toast);
        setTimeout(() => $toast.addClass('show'), 10);
        setTimeout(() => {
            $toast.removeClass('show');
            setTimeout(() => $toast.remove(), 300);
        }, 2000);
    }
    async function isUserLoggedIn() {
        try {
            const response = await fetch('/api/profile/info', {
                credentials: 'include' // ensures cookies are sent with the request
            });
            return response.ok; // true if 200 OK
        } catch (error) {
            console.error('API call failed:', error);
            return false;
        }
    }

  

    // Add new function to show share dialog
    async function showShareDialog(entityName, recordId) {
        const $dialog = $(`
        <div id="share-dialog" class="modal modal-open">
            <div class="modal-box">
                <h3 class="font-bold text-lg">Share</h3>
                <div class="mt-4 flex flex-col gap-2">
                    <button class="btn btn-outline share-option" data-platform="x">
                        <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
                            <!-- X logo SVG -->
                            <path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z"/>
                        </svg>
                        Share to X
                    </button>
                    <button class="btn btn-outline share-option" data-platform="email">
                        <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
                            <!-- Email icon SVG -->
                            <path d="M20 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 4l-8 5-8-5V6l8 5 8-5v2z"/>
                        </svg>
                        Share via Email
                    </button>
                    <button class="btn btn-outline share-option" data-platform="reddit">
                        <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
                            <!-- Reddit logo SVG -->
                            <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm4.18 5.97c.57 0 1.03.46 1.03 1.03 0 .28-.11.53-.29.71-.37.37-.96.42-1.37.11-.15-.11-.25-.28-.29-.46-.05-.28-.02-.58.11-.83.18-.37.54-.56.81-.56zm-8.36 0c.27 0 .63.19.81.56.13.25.16.55.11.83-.04.18-.14.35-.29.46-.41.31-1 .26-1.37-.11-.18-.18-.29-.43-.29-.71 0-.57.46-1.03 1.03-1.03zm4.18 2.03c-1.38 0-2.5 1.12-2.5 2.5s1.12 2.5 2.5 2.5 2.5-1.12 2.5-2.5-1.12-2.5-2.5-2.5zm0 3.5c-.55 0-1-.45-1-1s.45-1 1-1 1 .45 1 1-.45 1-1 1zm4.15-1.47c-.18 0-.36.05-.52.15-.55.34-1.22.52-1.93.52s-1.38-.18-1.93-.52c-.16-.1-.34-.15-.52-.15-.55 0-1 .45-1 1 0 .27.11.52.29.71.55.58 1.38.96 2.36.96s1.81-.38 2.36-.96c.18-.19.29-.44.29-.71 0-.55-.45-1-1-1z"/>
                        </svg>
                        Share to Reddit
                    </button>
                    <button class="btn btn-outline share-option" data-platform="clipboard">
                        <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
                            <!-- Clipboard icon SVG -->
                            <path d="M16 1H4c-1.1 0-2 .9-2 2v14h2V3h12V1zm3 4H8c-1.1 0-2 .9-2 2v14c NA0 1.1.9 2 2 2h11c1.1 0 2-.9 2-2V7c0-1.1-.9-2-2-2zm0 16H8V7h11v14z"/>
                        </svg>
                        Copy Link
                    </button>
                </div>
                <div class="modal-action">
                    <button id="cancel-share" class="btn">Cancel</button>
                </div>
            </div>
        </div>
    `);

        $('body').append($dialog);

        // Handle share options
        $dialog.find('.share-option').on('click', async function () {
            const platform = $(this).data('platform');
            const url = window.location.href;
            const title = document.title || 'Check this out!';

            try {
                await recordActivity(entityName, recordId, 'share');

                switch (platform) {
                    case 'x':
                        window.open(`https://x.com/intent/tweet?url=${encodeURIComponent(url)}&text=${encodeURIComponent(title)}`, '_blank');
                        break;
                    case 'email':
                        window.location.href = `mailto:?subject=${encodeURIComponent(title)}&body=${encodeURIComponent(url)}`;
                        break;
                    case 'reddit':
                        window.open(`https://www.reddit.com/submit?url=${encodeURIComponent(url)}&title=${encodeURIComponent(title)}`, '_blank');
                        break;
                    case 'clipboard':
                        await navigator.clipboard.writeText(url);
                        showToast('Link copied to clipboard');
                        break;
                }

                if (platform !== 'clipboard') {
                    showToast(`Shared to ${platform.charAt(0).toUpperCase() + platform.slice(1)}`);
                }else {
                    showToast(`Copied to clipboard`);
                }
                $dialog.remove();
            } catch (err) {
                console.error('Error sharing:', err);
                showToast('Failed to share');
            }
        });

        // Handle cancel
        $('#cancel-share').on('click', () => $dialog.remove());
    }
});
