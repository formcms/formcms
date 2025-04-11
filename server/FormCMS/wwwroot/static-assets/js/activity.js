$(document).ready(function () {
    loadActivity()

    function loadActivity() {
        $(`[data-component="activity-bar"]`).each(function () {
            const entityName = $(this).data('entity');
            const recordId = $(this).data('record-id');
            const $activityBar = $(this);
            const likeButton = $activityBar.find('[data-component="like-button"]');
            const saveButton = $activityBar.find('[data-component="save-button"]');
            const shareButton = $activityBar.find('[data-component="share-button"]');
            const viewButton = $activityBar.find('[data-component="view-button"]');

            $.ajax({
                url: `/api/activities/${entityName}/${recordId}`,
                method: 'GET',
                success: function (data) {
                    // Update Like button
                    likeButton.find('[data-component="like-count"]').text(formatCount(data.like.count));
                    likeButton.toggleClass('active', data.like.active);
                    likeButton.find('[data-component="like-icon-inactive"]').toggleClass('hidden', data.like.active);
                    likeButton.find('[data-component="like-icon-active"]').toggleClass('hidden', !data.like.active);

                    // Update Save button
                    saveButton.toggleClass('active', data.save.active);
                    saveButton.find('[data-component="save-icon-inactive"]').toggleClass('hidden', data.save.active);
                    saveButton.find('[data-component="save-icon-active"]').toggleClass('hidden', !data.save.active);

                    // Update View count (incremented by backend)
                    viewButton.find('[data-component="view-count"]').text(formatCount(data.view.count));

                    // Update Share button
                    shareButton.toggleClass('active', data.share.active);
                    shareButton.find('[data-component="share-icon-inactive"]').toggleClass('hidden', data.share.active);
                    shareButton.find('[data-component="share-icon-active"]').toggleClass('hidden', !data.share.active);
                },
                error: function (xhr, status, error) {
                    console.error('Error fetching activity data:', error);
                }
            });

            likeButton.on('click', function (e) {
                e.preventDefault();
                const currentActive = $(this).hasClass('active');
                const newActive = !currentActive;

                $.ajax({
                    url: `/api/activities/toggle/${entityName}/${recordId}?type=like&active=${newActive}`,
                    method: 'POST',
                    success: function (newCount) {
                        likeButton.find('[data-component="like-count"]').text(formatCount(newCount));
                        likeButton.toggleClass('active', newActive);
                        likeButton.find('[data-component="like-icon-inactive"]').toggleClass('hidden', newActive);
                        likeButton.find('[data-component="like-icon-active"]').toggleClass('hidden', !newActive);
                    },
                    error: function (xhr, status, error) {
                        console.error('Error toggling like:', error);
                    }
                });
            });

            // Share button click handler
            shareButton.on('click', function (e) {
                e.preventDefault();

                navigator.clipboard.writeText(window.location.href).then(() => {
                    showToast('Address has been copied to clipboard');

                    $.ajax({
                        url: `/api/activities/record/${entityName}/${recordId}?type=share`,
                        method: 'POST',
                        success: function (response) {
                            // Update Share button
                            shareButton.toggleClass('active', true);
                            shareButton.find('[data-component="share-icon-inactive"]').toggleClass('hidden', true);
                            shareButton.find('[data-component="share-icon-active"]').toggleClass('hidden', false);
                        },
                        error: function (xhr, status, error) {
                            console.error('Error recording share:', error);
                        }
                    });
                }).catch(err => {
                    console.error('Failed to copy to clipboard:', err);
                });
            });

            // Save button click handler
            saveButton.on('click', function (e) {
                e.preventDefault();
                const currentActive = $(this).hasClass('active');
                const newActive = !currentActive;

                $.ajax({
                    url: `/api/activities/toggle/${entityName}/${recordId}?type=save&active=${newActive}`,
                    method: 'POST',
                    success: function (response) {
                        saveButton.toggleClass('active', newActive);
                        saveButton.find('[data-component="save-icon-inactive"]').toggleClass('hidden', newActive);
                        saveButton.find('[data-component="save-icon-active"]').toggleClass('hidden', !newActive);
                    },
                    error: function (xhr, status, error) {
                        console.error('Error toggling save:', error);
                    }
                });
            });
        });
    }

    function showToast(message) {
        // Remove existing toast if any
        $('.toast').remove();

        // Create toast element
        const $toast = $('<div class="toast"></div>').text(message);
        $('body').append($toast);

        // Show toast with animation
        setTimeout(() => {
            $toast.addClass('show');
        }, 10); // Small delay to trigger transition

        // Hide toast after 2 seconds
        setTimeout(() => {
            $toast.removeClass('show');
            setTimeout(() => $toast.remove(), 300); // Wait for fade-out animation
        }, 2000);
    }

    function formatCount(count) {
        if (count >= 1000) {
            const thousands = (count / 1000).toFixed(1);
            return thousands.endsWith('.0') ? thousands.slice(0, -2) + 'k' : thousands + 'k';
        }
        return count.toString();
    }
})