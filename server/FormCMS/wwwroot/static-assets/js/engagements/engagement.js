import {formatCount} from "../utils/formatter.js";
import {showToast} from "../utils/toast.js";
import {ensureUser, getUser} from "../utils/user.js";

import {
    fetchActivity, fetchActivityStatus,
    fetchBookmarkFolders,
    recordActivity,
    saveBookmark,
    toggleActivity
} from "../services/engagementService.js";
import {shareDialogHTML} from "./components/shareDiaogHtml.js";
import {bookmarkDialogHtml} from "./components/bookmarkDialogHtml.js";

export function renderEngagementBar(element) {
    if (!document.getElementById('my-style')) {
        const style = document.createElement('style');
        style.id = 'my-style';
        style.textContent = `
        .active svg {
            fill: currentColor;
        }

        .inactive svg {
            fill: none;
        }
    `;
        document.head.appendChild(style);
    }
    element.querySelectorAll('[data-component="activity-bar"]').forEach(loadActivityBar);
    element.querySelectorAll('[data-batch-fetch-engagement]').forEach(loadBatchStatus);
}

export async function loadBatchStatus(ele){
    const bars = ele?.querySelectorAll('[data-component="activity-bar"]');
    const activity = ele.dataset.batchFetchStatusActivity;
    const entity = ele.dataset.entity;
    
    if ( bars.length > 0) {
        const recordIds = Array.from(bars).map(element => element.getAttribute('data-record-id'));
        const activeIds = await fetchActivityStatus(entity,activity,recordIds);
        console.info(entity, activity,recordIds,activeIds);
        bars.forEach((bar) => {
            if (activeIds.includes(+bar.dataset.recordId)) {
                console.log(bar.dataset.recordId);
                const btn = bar.querySelector(`[data-component="${activity}-button"]`);
                btn.classList.toggle('active', true);
            }
        })
    }
}
export async function loadActivityBar(activityBar) {
    const entityName = activityBar.dataset.entity;
    const recordId = activityBar.dataset.recordId;
    const fetchIndividualStatus = activityBar.dataset.fetchIndividualStatus;

    const likeButton = activityBar.querySelector('[data-component="like-button"]');
    const saveButton = activityBar.querySelector('[data-component="save-button"]');
    const shareButton = activityBar.querySelector('[data-component="share-button"]');
    const viewButton = activityBar.querySelector('[data-component="view-button"]');

    loadActivityListeners(entityName, recordId, likeButton, saveButton, shareButton);

    if (fetchIndividualStatus !== 'no') {
        await loadActivityStatusAndCounts(entityName, recordId, viewButton, likeButton);
    }
}

async function loadActivityStatusAndCounts(entityName, recordId, viewButton, likeButton) {
    try {
        const data = await fetchActivity(entityName, recordId);
        updateLikeButton(likeButton, data.like);
        updateViewButton(viewButton, data.view);
    } catch (err) {
        console.error('Error loading activity:', err);
    }
}

function loadActivityListeners(entityName, recordId, likeButton, saveButton, shareButton) {
    if (likeButton) {
        likeButton.addEventListener('click', async function (e) {
            e.preventDefault();
            await handleLikeButtonClick(likeButton, entityName, recordId);
        });
    }

    if (shareButton) {
        shareButton.addEventListener('click', async function (e) {
            e.preventDefault();
            await showShareDialog(entityName, recordId);
        });
    }

    if (saveButton) {
        saveButton.addEventListener('click', async function (e) {
            e.preventDefault();
            await showBookmarkModal(entityName, recordId);
        });
    }
}

async function handleLikeButtonClick(likeButton,entityName, recordId) {
    if (!ensureUser()) return;

    const active = likeButton.classList.contains('active');
    try {
        const newCount = await toggleActivity(entityName, recordId, 'like', !active);
        updateLikeButton(likeButton, {count: newCount, active: !active});
    } catch (err) {
        console.error('Error toggling like:', err);
    }   
}

function updateLikeButton(btn, data) {
    btn.classList.toggle('active', data.active);
    const countSpan = btn.querySelector('[data-component="like-count"]');
    if (countSpan) countSpan.textContent = formatCount(data.count);
}

function updateViewButton(btn, data) {
    const countSpan = btn.querySelector('[data-component="view-count"]');
    if (countSpan) countSpan.textContent = formatCount(data.count);
}

async function showBookmarkModal(entityName, recordId) {
    if (!getUser()) return;

    // Create modal
    const modal = document.createElement('div');
    modal.id = 'bookmark-modal';
    modal.className = 'modal modal-open';
    modal.innerHTML = bookmarkDialogHtml;
    document.body.appendChild(modal);

    try {
        const folders = await fetchBookmarkFolders(entityName, recordId);
        const folderList = document.getElementById('folder-list');

        folders.forEach(folder => {
            const container = document.createElement('div');
            container.className = 'form-control';
            container.innerHTML = `
                <label class="label cursor-pointer">
                    <span class="label-text">${folder.name || 'Default Folder'}</span>
                    <input type="checkbox" class="checkbox" data-folder-id="${folder.id}" ${folder.selected ? 'checked' : ''} />
                </label>
            `;
            folderList.appendChild(container);
        });
    } catch (err) {
        showToast('Failed to load bookmark folders');
        modal.remove();
        return;
    }

    document.getElementById('save-bookmark').addEventListener('click', async () => {
        const checkboxes = document.querySelectorAll('#folder-list input[type="checkbox"]:checked');
        const selectedFolders = Array.from(checkboxes).map(cb => parseInt(cb.getAttribute('data-folder-id')));
        const newFolderName = document.getElementById('new-folder-name').value;

        try {
            await saveBookmark(entityName, recordId, {selectedFolders, newFolderName});
            showToast('Bookmarked successfully, you can see bookmarked items in User Portal');
            modal.remove();
        } catch (err) {
            showToast('Failed to save bookmark');
        }
    });

    document.getElementById('cancel-bookmark').addEventListener('click', () => {
        modal.remove();
    });
}

async function showShareDialog(entityName, recordId) {
    const tempDiv = document.createElement('div');
    tempDiv.innerHTML = shareDialogHTML;
    const dialog = tempDiv.firstElementChild;
    document.body.appendChild(dialog);

    // Event delegation for share buttons
    dialog.querySelectorAll('.share-option').forEach(button => {
        button.addEventListener('click', async () => {
            const platform = button.dataset.platform;
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
                }
                dialog.remove();
            } catch (err) {
                console.error('Error sharing:', err);
                showToast('Failed to share');
            }
        });
    });

    // Cancel button handler
    dialog.querySelector('#cancel-share').addEventListener('click', () => {
        dialog.remove();
    });
}
