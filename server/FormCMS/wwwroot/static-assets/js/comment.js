import {showToast} from "./utils/toast.js";
import {fetchUser, getUser} from "./utils/user.js";
import {fetchPagePart, initIntersectionObserver} from "./utils/datalist.js";
import {deleteComments, saveComments, updateComments} from "./services/commentService.js";

const dataLists = document.querySelectorAll('[data-component="data-list"]');
dataLists.forEach(loadComments);
fetchUser().then(user=>{
    dataLists.forEach(enableCommentEditing);
});

function enableCommentEditing(dataList) {
    const commentContainers = dataList.querySelectorAll('[data-component="comment-container"]');
    commentContainers.forEach(commentContainer => {
        const user = getUser();
        const id = commentContainer.dataset.id;
        const userId= commentContainer.dataset.userId;
        const editButton = commentContainer.querySelector('[data-component="comment-edit"]');
        const delButton = commentContainer.querySelector('[data-component="comment-del"]');
        if (userId !== user?.id) {
            editButton.remove();
            delButton.remove();
            return;
        }
        
        delButton.addEventListener('click', async (event)=>{
           if (confirm("Do you want to delete this comment?")) {
               await deleteComments(id);
               commentContainer.remove();
           } 
        });

        editButton.addEventListener('click', function () {
            // Find the closest comment container using data-component
            const commentTextElement = commentContainer.querySelector('[data-component="comment-content"]');
            const originalText = commentTextElement.textContent.trim();

            // Prevent multiple edit modes
            if (commentContainer.querySelector('textarea')) return;

            // Create textarea
            const textarea = document.createElement('textarea');
            textarea.className = 'w-full p-2 border border-gray-300 rounded-md text-sm text-gray-600';
            textarea.value = originalText;

            // Create Save and Cancel buttons
            const saveButton = document.createElement('button');
            saveButton.className = 'btn btn-primary btn-sm mr-2';
            saveButton.textContent = 'Save';
            const cancelButton = document.createElement('button');
            cancelButton.className = 'btn btn-ghost btn-sm';
            cancelButton.textContent = 'Cancel';

            // Create error message container
            const errorMessage = document.createElement('p');
            errorMessage.className = 'text-xs text-red-500 mt-1 hidden';
            errorMessage.textContent = 'Failed to save comment. Please try again.';

            // Create a container for buttons
            const buttonContainer = document.createElement('div');
            buttonContainer.className = 'flex gap-2 mt-2';
            buttonContainer.appendChild(saveButton);
            buttonContainer.appendChild(cancelButton);

            // Append elements
            commentTextElement.style.display = 'none';
            const parentContainer = commentTextElement.parentElement;
            parentContainer.insertBefore(textarea, commentTextElement.nextSibling); // Insert textarea in its place
            parentContainer.appendChild(errorMessage);
            parentContainer.appendChild(buttonContainer);

            // Focus the textarea
            textarea.focus();

            // Function to restore UI
            function restoreUI() {
                commentTextElement.style.display = 'block';
                textarea.remove();
                buttonContainer.remove();
                errorMessage.remove();
            }

            // Save button click handler
            saveButton.addEventListener('click', async function () {
                const newText = textarea.value.trim();
                if (!newText) {
                    errorMessage.textContent = 'Comment cannot be empty.';
                    errorMessage.classList.remove('hidden');
                    return;
                }

                // Send update to server
                const result = await updateComments(id,newText);

                if (result?.error) {
                    errorMessage.textContent = result.error;
                    errorMessage.classList.remove('hidden');
                } else {
                    commentTextElement.textContent = newText;
                    restoreUI();
                }
            });

            // Cancel button click handler
            cancelButton.addEventListener('click', function () {
                restoreUI();
            });

            // Handle Enter and Escape keys for accessibility
            textarea.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' && !e.shiftKey) {
                    saveButton.click();
                    e.preventDefault();
                } else if (e.key === 'Escape') {
                    cancelButton.click();
                }
            });
        });
    });
}

function loadComments(dataList) {
    const commentForm = dataList.querySelector('[data-component="comment-form"]');
    const foreachElement = dataList.querySelector('[data-component="foreach"]');
    if (!commentForm) return;

    const commentText = commentForm.querySelector('[data-component="comment-text"]');
    const entityName = commentForm.dataset.entity;
    const recordId = commentForm.dataset.recordId;
    
    commentText.addEventListener('focus', function () {
        if (!getUser()) { // Reference global user
            const proceed = confirm("You must log in to comment. Do you want to log in now?");
            if (proceed) {
                window.location.href = "/portal?ref=" + encodeURIComponent(window.location.href);
            } else {
                this.blur();
            }
        }
    });

    // Rest of the code remains the same
    commentForm.addEventListener('submit', async function (e) {
        e.preventDefault();
        const text = commentText.value.trim();
        if (text) {
            try {
                await saveComments(entityName, recordId, text);
                await reloadDataList(foreachElement);
                commentForm.reset();
                showToast('Comment added!');
            } catch (error) {
                showToast('Failed to add comment: ' + error.message);
            }
        } else {
            showToast('Please fill comment.');
        }
    })

    async function reloadDataList(foreachElement) {
        const token = foreachElement.getAttribute("first_page");
        const sourceId = foreachElement.getAttribute("source_id");
        const html = await fetchPagePart(sourceId, token, true);
        foreachElement.innerHTML = html;
        await initIntersectionObserver();
    }
}