import {currentUser, showToast, fetchPagePart, initIntersectionObserver} from "./utils.js";

$(document).ready(function () {
    let user = false;
    currentUser().then(x => user = x);
    $(`[data-component="data-list"]`).each(loadComments);


    async function reloadDataList($foreach) {
        const token = $foreach.attr("first_page");
        const sorceId = $foreach.attr("source_id");
        const html = await fetchPagePart(sorceId, token, true);
        $foreach.html(html);
        await initIntersectionObserver();
    }
    
    function loadComments() {
        const dataList = $(this);
        const $commentForm = dataList.find(`[data-component="comment-form"]`);
        const $foreach = dataList.find(`[data-component="foreach"]`);
        if (!$commentForm || !$foreach) return;
        
        
        
        const $commentText = $commentForm.find(`[data-component="comment-text"]`);
        const entityName = $commentForm.data('entity');
        const recordId = $commentForm.data('record-id');
        
        $commentText.on('focus', function () {
            if (!user) {
                alert("Please login first");
                window.location.href = "/portal?ref=" + encodeURIComponent(window.location.href);
            }
        });

        // Handle form submission
        $commentForm.on('submit', async function (e) {
            e.preventDefault();
            const text = $commentText.val().trim();
            if (text) {
                const commentData = {
                    EntityName: entityName,
                    RecordId: recordId,
                    Content: text,
                };

                try {
                    const response = await fetch('/api/comments', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'Accept': 'application/json'
                        },
                        body: JSON.stringify(commentData)
                    });
                    if (!response.ok) {
                        throw new Error(`HTTP error! Status: ${response.status}`);
                    }
                    await reloadDataList($foreach)
                    $commentForm[0].reset();
                    showToast('Comment added!');
                } catch (error) {
                    showToast('Failed to add comment: ' + error.message);
                }
            } else {
                showToast('Please fill comment.');
            }
        });
    }
});