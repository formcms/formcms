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
<div class="py-6 px-8 mx-auto max-w-screen-lg" 
    data-gjs-type="data-list"  
    data-component="data-list" 
    field="comments" 
    offset="0" 
    limit="5"
    >
    <h3 class="sm:text-2xl text-2xl font-bold title-font mb-4 text-gray-900">Comments</h3>
    <div class="mb-6">
        <form data-component="comment-form" data-gjs-type="comment-form" class="flex flex-col gap-4 py-1"  data-record-id="{{id}}">
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
        <div class="bg-white p-4 rounded-lg shadow-sm border border-gray-200" data-component='comment-container' data-id="{{id}}" data-user-id="{{user.id}}">
             <div class="flex items-start" >
                  <div class="w-10 h-10 rounded-full bg-gray-300 flex items-center justify-center mr-3 overflow-hidden">
                       <img src="{{user.avatarUrl}}" alt="User Avatar" class="w-full h-full object-cover">
                  </div>
                  <div class="flex-1">
                       <p class="text-xs text-gray-400 mt-1">{{user.name}} · {{createdAt}}</p>
                       <p data-component="comment-content" class="text-sm text-gray-600">{{content}}</p>
                  </div>
             </div>
                  
             <div class="flex items-center gap-2 mt-2">
                 <button data-component="comment-like" class="btn btn-ghost btn-sm flex items-center gap-1">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z"></path></svg>
                        Like
                 </button>
                 <span class="text-xs text-gray-500">(<span data-component="like-count">0</span>)</span>
                 <button data-component="comment-reply" class="btn btn-ghost btn-sm flex items-center gap-1">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h10a8 8 0 018 8v2M3 10l6 6m0-12l-6 6"></path></svg>
                        Reply
                 </button>
                 <span class="text-xs text-gray-500">(<span data-component="reply-count">0</span>)</span>
                 <button data-component="comment-edit" class="btn btn-ghost btn-sm flex items-center gap-1">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z"></path></svg>
                        Edit
                 </button>
                 <button data-component="comment-del" class="btn btn-ghost btn-sm flex items-center gap-1">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5-4h4"></path></svg>
                        Delete
                 </button>
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