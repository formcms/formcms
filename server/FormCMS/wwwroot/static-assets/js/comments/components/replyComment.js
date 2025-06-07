import {replyComments} from "../../services/commentService.js";
import {showToast} from "../../utils/toast.js";
import {reloadDataList} from "../../utils/datalist.js";

export async function showReply(commentContainer){
    const replyList  = commentContainer.querySelector('[data-component="data-list"]');
    await reloadDataList(replyList); 
}

export async function replyComment (commentContainer,id) {
    if (commentContainer.querySelector('textarea')) return;
    
    const replyForm = document.createElement('form');
    replyForm.className = 'mt-2 p-2 border border-gray-300 rounded-md';

    const textarea = document.createElement('textarea');
    textarea.className = 'w-full p-2 border border-gray-300 rounded-md text-sm text-gray-600';
    textarea.placeholder = 'Write your reply...';

    const submitButton = document.createElement('button');
    submitButton.className = 'btn btn-primary btn-sm mr-2';
    submitButton.textContent = 'Submit Reply';
    submitButton.type = 'submit';

    const cancelButton = document.createElement('button');
    cancelButton.className = 'btn btn-ghost btn-sm';
    cancelButton.textContent = 'Cancel';
    cancelButton.type = 'button';

    const errorMessage = document.createElement('p');
    errorMessage.className = 'text-xs text-red-500 mt-1 hidden';
    errorMessage.textContent = 'Failed to save reply. Please try again.';

    const buttonContainer = document.createElement('div');
    buttonContainer.className = 'flex gap-2 mt-2';
    buttonContainer.appendChild(submitButton);
    buttonContainer.appendChild(cancelButton);

    replyForm.appendChild(textarea);
    replyForm.appendChild(errorMessage);
    replyForm.appendChild(buttonContainer);

    commentContainer.appendChild(replyForm);

    textarea.focus();

    function removeReplyForm() {
        replyForm.remove();
    }

    replyForm.addEventListener('submit', async function (e) {
        e.preventDefault();
        const replyText = textarea.value.trim();
        if (!replyText) {
            errorMessage.textContent = 'Reply cannot be empty.';
            errorMessage.classList.remove('hidden');
            return;
        }
        try {
            await replyComments(id,replyText);
            removeReplyForm();
            await showReply(commentContainer)
            
           
            showToast('Reply added!');
        } catch (error) {
            errorMessage.textContent = 'Failed to save reply: ' + error.message;
            errorMessage.classList.remove('hidden');
        }
    });

    cancelButton.addEventListener('click', function () {
        removeReplyForm();
    });

    textarea.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && !e.shiftKey) {
            submitButton.click();
            e.preventDefault();
        } else if (e.key === 'Escape') {
            cancelButton.click();
        }
    });
}