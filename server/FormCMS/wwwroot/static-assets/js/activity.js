$(document).ready(function () {
    let loggedIn = false;
    isUserLoggedIn().then(x=> loggedIn = x);
    
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

        shareButton.on('click', async function (e) {
            e.preventDefault();
            try {
                await navigator.clipboard.writeText(window.location.href);
                showToast('Address has been copied to clipboard');
                await recordActivity(entityName, recordId, 'share');
            } catch (err) {
                console.error('Error sharing:', err);
            }
        });

        saveButton.on('click', () => showBookmarkModal(entityName, recordId));
    }
    // API Functions
    async function fetchActivity(entity, id) {
        const res = await fetch(`/api/activities/${entity}/${id}`);
        return await res.json();
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
});
