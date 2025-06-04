import {showToast} from "./utils/formatter.js";
import {fetchUser, getUser} from "./utils/user.js";
import {fetchPagePart, initIntersectionObserver} from "./utils/datalist.js";
import {saveComments} from "./services/commentService.js";

const dataLists = document.querySelectorAll('[data-component="data-list"]');
dataLists.forEach(loadComments);
fetchUser();

function loadComments(dataList) {
    const commentForm = dataList.querySelector('[data-component="comment-form"]');
    const foreachElement = dataList.querySelector('[data-component="foreach"]');
    if (!commentForm) return;
    console.log(commentForm);

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