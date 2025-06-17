import {replyComment, showReply} from "./replyComment.js";
import {deleteComments} from "../../services/commentService.js";
import {editComment} from "./editComment.js";
import {getUser} from "../../utils/user.js";

export function initCommentButtons(dataList, render) {
    const user = getUser();
    const foreach = dataList.querySelector(':scope > [data-component="foreach"]');
    const commentContainers = foreach.querySelectorAll(':scope > [data-component="comment-container"]');

    commentContainers.forEach(commentContainer => {
        const id = commentContainer.getAttribute('__record_id');
        const activityBar = commentContainer.querySelector(':scope > [data-component="activity-bar"]');
        
        const replyButton = activityBar.querySelector(':scope > [data-component="comment-reply"]');
        if (replyButton) replyButton.addEventListener('click', () => replyComment(commentContainer, id,  render));

        const viewReplyButton = activityBar.querySelector(':scope > [data-component="view-reply"]');
        if (viewReplyButton) viewReplyButton.addEventListener('click',async () =>await showReply(commentContainer,render));

        const userId = commentContainer.dataset.userId;
        const editButton = activityBar.querySelector(':scope > [data-component="comment-edit"]');
        const delButton = activityBar.querySelector(':scope > [data-component="comment-del"]');

        if (userId !== user?.id) {
            if (editButton) editButton.remove();
            if (delButton) delButton.remove();
        } else {
            if (editButton) delButton.addEventListener('click', async (event) => {
                if (confirm("Do you want to delete this comment?")) {
                    await deleteComments(id);
                    commentContainer.remove();
                }
            });
            if (delButton) editButton.addEventListener('click', () => editComment(commentContainer,id));
        }
    });
}