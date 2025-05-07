export const activityBar =
    {
        category: 'Data Display',
        name: 'activity-bar',
        label: 'Activity Bar',
        media: `<svg viewBox="0 0 24 24" class="icon" xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
      <!-- Heart for Like (top-left, scaled down) -->
        <path d="M6 8.5l-.7-.6C3.4 6.3 2 5.2 2 3.5 2 2.1 3.1 1 4.5 1c.8 0 1.6.4 2 1 .4-.6 1.2-1 2-1 1.4 0 2.5 1.1 2.5 2.5 0 1.7-1.5 3.1-3.8 5.2L6 8.5z" />
        <!-- Share arrow (top-right, scaled down) -->
        <path d="M18 6l2 2m0 0l-2 2m2-2h-4" />
        <!-- Eye pupil for View (bottom-center, scaled down) -->
        <circle cx="12" cy="16" r="1" />
        <!-- Eye outline for View (bottom-center, scaled down) -->
        <path d="M17 16c0-2.2-1.8-4-4-4s-4 1.8-4 4" /> 
    </svg>`,
        content: `
    <div class="btn-group flex justify-center gap-2"  data-component="activity-bar" data-gjs-type="activity-bar">
        <button data-component="like-button" class="btn btn-primary btn-sm flex items-center gap-1">
            <!-- Inactive: Outline Heart -->
            <svg data-component="like-icon-inactive" xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
            </svg>
            <!-- Active: Filled Heart (hidden by default) -->
            <svg data-component="like-icon-active" xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 hidden" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M3.172 5.172a4 4 0 015.656 0L10 6.343l1.172-1.171a4 4 0 115.656 5.656L10 17.657l-6.828-6.829a4 4 0 010-5.656z" clip-rule="evenodd" />
            </svg>
            <span data-component="like-count">234</span>
        </button>

        <button data-component="share-button" class="btn btn-secondary btn-sm flex items-center gap-1">
            <svg data-component="share-icon-active" xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 hidden" viewBox="0 0 20 20" fill="currentColor">
                <path d="M15 8a3 3 0 10-2.097-2.144 1 1 0 00-.683-.276h-1.44a1 1 0 00-.683.276l-2.914 2.914a1 1 0 01-1.414 0l-2.914-2.914A1 1 0 003.22 5.58 3 3 0 101 8l3.293 3.293a1 1 0 001.414 0L9 8l3.293 3.293a1 1 0 001.414 0L17 8z" />
            </svg>
            <span>Share</span>
        </button>

        <button data-component="save-button" class="btn btn-accent btn-sm flex items-center gap-1">
            <svg data-component="save-icon-active" xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 hidden" viewBox="0 0 20 20" fill="currentColor">
                <path d="M5 4a2 2 0 012-2h6a2 2 0 012 2v14l-5-2.5L5 18V4z" />
            </svg>
            <span>Save</span>
        </button>

        <!-- Non-clickable view count with inline style -->
        <button disabled style="opacity: 1; background-color: inherit; border-color: inherit; color: inherit; cursor: default;"
                data-component="view-button" class="btn btn-info btn-sm flex items-center gap-1">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
            </svg>
            <span data-component="view-count">1.5k</span>
        </button>
    </div>
 `
    }