import {fetchUser, getUser} from "../utils/user.js";
import {deleteComments} from "../services/commentService.js";
import {editComment} from "./components/editComment.js";
import {replyComment} from "./components/replyComment.js";
import {loadComments} from "./components/loadComments.js";

const dataLists = document.querySelectorAll('[data-component="data-list"]');
dataLists.forEach(loadComments);
fetchUser().then(user=>{
    dataLists.forEach(enableCommentEditing);
});

function enableCommentEditing(dataList) {
    const commentContainers = dataList.querySelectorAll('[data-component="comment-container"]');
    commentContainers.forEach(commentContainer => {
        const user = getUser();
        const replyButton = commentContainer.querySelector('[data-component="comment-reply"]');
        replyButton.addEventListener('click', ()=> replyComment(commentContainer));
        
        const viewReplyButton = commentContainer.querySelector('[data-component="view-reply"]');
        replyButton.addEventListener('click', ()=> {
            
        });
        
        
        const userId= commentContainer.dataset.userId;
        const editButton = commentContainer.querySelector('[data-component="comment-edit"]');
        const delButton = commentContainer.querySelector('[data-component="comment-del"]');
        
        if (userId !== user?.id) {
            editButton.remove();
            delButton.remove();
        }else {
            delButton.addEventListener('click', async (event) => {
                if (confirm("Do you want to delete this comment?")) {
                    await deleteComments(id);
                    commentContainer.remove();
                }
            });
            editButton.addEventListener('click', () => editComment(commentContainer));
        }
    });
}