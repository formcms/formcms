import {updateComments} from "../../services/commentService.js";

export function editComment(commentContainer,id) {
    const commentTextElement = commentContainer.querySelector('[data-component="comment-content"]');
    const originalText = commentTextElement.textContent.trim();

    if (commentContainer.querySelector('textarea')) return;

    const textarea = document.createElement('textarea');
    textarea.className = 'w-full p-2 border border-gray-300 rounded-md text-sm text-gray-600';
    textarea.value = originalText;

    const saveButton = document.createElement('button');
    saveButton.className = 'btn btn-primary btn-sm mr-2';
    saveButton.textContent = 'Save';
    const cancelButton = document.createElement('button');
    cancelButton.className = 'btn btn-ghost btn-sm';
    cancelButton.textContent = 'Cancel';

    const errorMessage = document.createElement('p');
    errorMessage.className = 'text-xs text-red-500 mt-1 hidden';
    errorMessage.textContent = 'Failed to save comment. Please try again.';

    const buttonContainer = document.createElement('div');
    buttonContainer.className = 'flex gap-2 mt-2';
    buttonContainer.appendChild(saveButton);
    buttonContainer.appendChild(cancelButton);

    commentTextElement.style.display = 'none';
    const parentContainer = commentTextElement.parentElement;
    parentContainer.insertBefore(textarea, commentTextElement.nextSibling); // Insert textarea in its place
    parentContainer.appendChild(errorMessage);
    parentContainer.appendChild(buttonContainer);

    textarea.focus();

    function restoreUI() {
        commentTextElement.style.display = 'block';
        textarea.remove();
        buttonContainer.remove();
        errorMessage.remove();
    }

    saveButton.addEventListener('click', async function () {
        const newText = textarea.value.trim();
        if (!newText) {
            errorMessage.textContent = 'Comment cannot be empty.';
            errorMessage.classList.remove('hidden');
            return;
        }

        const result = await updateComments(id, newText);

        if (result?.error) {
            errorMessage.textContent = result.error;
            errorMessage.classList.remove('hidden');
        } else {
            commentTextElement.textContent = newText;
            restoreUI();
        }
    });

    cancelButton.addEventListener('click', function () {
        restoreUI();
    });

    textarea.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && !e.shiftKey) {
            saveButton.click();
            e.preventDefault();
        } else if (e.key === 'Escape') {
            cancelButton.click();
        }
    });
}