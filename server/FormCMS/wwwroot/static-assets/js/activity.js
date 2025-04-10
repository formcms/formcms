$(document).ready(function () {
    loadActivity()
    
    function loadActivity() {
        $(`[data-component="activity-bar"]`).each(function () {
            const entityName = $(this).data('entity');
            const recordId = $(this).data('record-id');
            $.ajax({
                url: `/api/activities/${entityName}/${recordId}`,
                method: 'GET',
                success: function(data) {
                    // Update Like button
                    const likeButton = $(this).find('.btn-primary');
                    likeButton.find('[data-component="like-count"]').text(data.like.count);
                    likeButton.toggleClass('active', data.like.active);
                    likeButton.find('svg').eq(0).toggleClass('hidden', data.like.active);
                    likeButton.find('svg').eq(1).toggleClass('hidden', !data.like.active);

                    // Update Save button
                    const saveButton = $(this).find('.btn-accent');
                    saveButton.toggleClass('active', data.save.active);
                    saveButton.find('svg').eq(0).toggleClass('hidden', data.save.active);
                    saveButton.find('svg').eq(1).toggleClass('hidden', !data.save.active);

                    // Update View button
                    const viewButton = $(this).find('.btn-info');
                    viewButton.find('[data-component="view-count"]').text(data.view.count);

                    // Update Share button
                    const shareButton = $(this).find('.btn-secondary');
                    shareButton.toggleClass('active', data.share.active);
                    shareButton.find('svg').eq(0).toggleClass('hidden', data.share.active);
                    shareButton.find('svg').eq(1).toggleClass('hidden', !data.share.active);
                }.bind(this), // Bind 'this' to maintain context
                error: function(xhr, status, error) {
                    console.error('Error fetching activity data:', error);
                }
            });

            // Add click handler for like button
            const likeButton = $(this).find('.btn-primary');
            likeButton.on('click', function(e) {
                e.preventDefault();
                const currentActive = $(this).hasClass('active');
                const newActive = !currentActive;

                $.ajax({
                    url: `/api/activities/toggle/${entityName}/${recordId}?type=like&active=${newActive}`,
                    method: 'POST',
                    success: function(newCount) {
                        // Assuming the API returns the updated data in the same format
                        likeButton.find('[data-component="like-count"]').text(newCount);
                        likeButton.toggleClass('active', newActive);
                        likeButton.find('svg').eq(0).toggleClass('hidden', newActive);
                        likeButton.find('svg').eq(1).toggleClass('hidden', !newActive);
                    },
                    error: function(xhr, status, error) {
                        console.error('Error toggling like:', error);
                    }
                });
            });
        });
    }
})