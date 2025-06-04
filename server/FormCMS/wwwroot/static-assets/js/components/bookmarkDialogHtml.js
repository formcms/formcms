export const bookmarkDialogHtml = `
        <div class="modal-box">
            <h3 class="font-bold text-lg">Save to Bookmark Folder</h3>
            <div id="folder-list" class="mt-4 max-h-60 overflow-y-auto"></div>
            <div class="mt-4">
                <div class="form-control">
                    <label class="label">
                        <span class="label-text">Create New Folder</span>
                    </label>
                    <div class="flex space-x-2">
                        <input id="new-folder-name" type="text" placeholder="Enter folder name" class="input input-bordered w-full" />
                    </div>
                </div>
            </div>
            <div class="modal-action">
                <button id="save-bookmark" class="btn btn-primary">Save</button>
                <button id="cancel-bookmark" class="btn">Cancel</button>
            </div>
        </div>
    `;