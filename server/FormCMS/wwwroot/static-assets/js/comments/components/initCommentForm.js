import {ensureUser} from "../../utils/user.js";
import {saveComments} from "../../services/commentService.js";
import {showToast} from "../../utils/toast.js";
import {reloadDataList} from "../../utils/datalist.js";


export function initCommentForm(dataList) {
    const commentForm = dataList.querySelector('[data-component="comment-form"]');
    if (!commentForm) return;

    const commentText = commentForm.querySelector('[data-component="comment-text"]');
    commentText.addEventListener('focus', function () {
        if (!ensureUser()) { 
            this.blur();
        }
    });

    // Rest of the code remains the same
    commentForm.addEventListener('submit', async function (e) {
        e.preventDefault();
        const entityName = commentForm.dataset.entity;

        const each = dataList.querySelector('[data-component="foreach"]');
        const recordId = each.getAttribute('__record_id');
        
        const text = commentText.value.trim();
        if (text) {
            try {
                await saveComments(entityName, recordId, text);
                await reloadDataList(dataList);
                commentForm.reset();
                showToast('Comment added!');
            } catch (error) {
                showToast('Failed to add comment: ' + error.message);
            }
        } else {
            showToast('Please fill comment.');
        }
    })
}