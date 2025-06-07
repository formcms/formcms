import {fetchUser} from "../utils/user.js";
import {deleteComments} from "../services/commentService.js";
import {editComment} from "./components/editComment.js";
import {replyComment, showReply} from "./components/replyComment.js";
import {loadComments} from "./components/loadComments.js";
import {reloadDataList} from "../utils/datalist.js";

const dataLists = document.querySelectorAll('[data-component="data-list"]');
dataLists.forEach(loadComments);
fetchUser().then(user => {
    dataLists.forEach((list) => enableCommentEditing(list, user));
});

function enableCommentEditing(dataList, user) {
    const commentContainers = dataList.querySelectorAll('[data-component="comment-container"]');
    commentContainers.forEach(commentContainer => {
        const id = commentContainer.getAttribute('__record_id');
        const replyButton = commentContainer.querySelector('[data-component="comment-reply"]');
        replyButton.addEventListener('click', () => replyComment(commentContainer,id));

        const viewReplyButton = commentContainer.querySelector('[data-component="view-reply"]');
        
        viewReplyButton.addEventListener('click',async () =>await showReply(commentContainer));

        const userId = commentContainer.dataset.userId;
        const editButton = commentContainer.querySelector('[data-component="comment-edit"]');
        const delButton = commentContainer.querySelector('[data-component="comment-del"]');

        if (userId !== user?.id) {
            editButton.remove();
            delButton.remove();
        } else {
            delButton.addEventListener('click', async (event) => {
                if (confirm("Do you want to delete this comment?")) {
                    await deleteComments(id);
                    commentContainer.remove();
                }
            });
            editButton.addEventListener('click', () => editComment(commentContainer,id));
        }
    });
}