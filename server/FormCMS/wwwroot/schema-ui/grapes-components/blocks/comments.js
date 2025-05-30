export const comments = {
    category: 'Data Display',
    name: 'comments',
    label: 'Comments',
    media: `
       <svg width="54" height="54" viewBox="0 0 54 54" fill="none" xmlns="http://www.w3.org/2000/svg">
        <rect x="2" y="2" width="50" height="50" rx="8" fill="#E5E7EB" />
        <path d="M15 16C15 14.8954 15.8954 14 17 14H37C38.1046 14 39 14.8954 39 16V30C39 31.1046 38.1046 32 37 32H22L17 37V32H15V16Z" fill="#3B82F6" />
        <rect x="19" y="18" width="16" height="2" rx="1" fill="#FFFFFF" />
        <rect x="19" y="22" width="12" height="2" rx="1" fill="#FFFFFF" />
        <rect x="19" y="26" width="14" height="2" rx="1" fill="#FFFFFF" />
      </svg>
    `,
    content: `
<div class="py-6 px-8 mx-auto max-w-screen-lg" data-gjs-type="data-list"  data-component="data-list" field="comments" offset="0" limit="5" >
    <h3 class="sm:text-2xl text-2xl font-bold title-font mb-4 text-gray-900">Comments</h3>
    <div class="mb-6">
        <form data-component="comment-form" data-gjs-type="comment-form" class="flex flex-col gap-4 py-1">
            <div class="flex items-center gap-4">
                <textarea data-component="comment-text" placeholder="Add a comment..." class="textarea textarea-bordered w-full" rows="3" required></textarea>
            </div>
            <div class="flex justify-end gap-2">
                <button type="reset" id="cancel-comment" class="btn btn-ghost btn-sm">Cancel</button>
                <button type="submit" id="submit-comment" class="btn btn-primary btn-sm">Comment</button>
            </div>
        </form>
    </div>
    <div class="space-y-4" data-gjs-type="foreach" data-component="foreach">
        <div class="bg-white p-4 rounded-lg shadow-sm border border-gray-200">
             <div class="flex items-start">
                  <div class="w-10 h-10 rounded-full bg-gray-300 flex items-center justify-center mr-3 overflow-hidden">
                       <img src="{{user.avatarUrl}}" alt="User Avatar" class="w-full h-full object-cover">
                  </div>
                  <div class="flex-1">
                       <p id="data" class="text-xs text-gray-400 mt-1">{{user.name}} · {{createdAt}}</p>
                       <p class="text-sm text-gray-600">{{content}}</p>
                  </div>
             </div>
        </div> 
    </div>
    <nav  data-gjs-type="pagination" aria-label="Pagination" class="flex justify-center space-x-3 mt-8">
       <a data-component="previous" class="relative inline-flex items-center px-5 py-2 text-sm bg-gradient-to-r from-violet-300 to-indigo-300 border border-fuchsia-100 hover:border-violet-100 font-semibold cursor-pointer leading-5 rounded-full transition duration-150 ease-in-out hover:scale-105 focus:outline-none focus:ring-2 focus:ring-blue-300" >Previous</a>
       <a data-component="next" class="relative inline-flex items-center px-5 py-2 text-sm bg-gradient-to-r from-violet-300 to-indigo-300 border border-fuchsia-100 hover:border-violet-100 font-semibold cursor-pointer leading-5 rounded-full transition duration-150 ease-in-out hover:scale-105 focus:outline-none focus:ring-2 focus:ring-blue-300"  >Next</a>
    </nav>
</div>
    `,
};