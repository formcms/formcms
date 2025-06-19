export const ecommerceA = {
        category: 'Data List',
        name: 'ecommerce-a',
        label: 'Ecommerce A',
        media: `<svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
    <path d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2 4h13" stroke="#000" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
    <circle cx="9" cy="21" r="1" fill="#000"/>
    <circle cx="20" cy="21" r="1" fill="#000"/>
  </svg>`,
        content: `
      <div class="container px-5 py-5 mx-auto text-gray-700 font-sans" data-gjs-type="data-list"  data-component="data-list"  offset="0" limit="4">
        <h3 class="text-3xl sm:text-4xl font-bold mb-8 text-gray-900">Ecommerce A</h3>
        <div class="flex flex-wrap -m-4" data-gjs-type="foreach" data-component="foreach">
          <div class="p-4 md:w-1/2 lg:w-1/4 w-full">
            <div class="bg-white rounded-xl overflow-hidden border border-gray-200 transition-transform duration-300 ease-in-out hover:-translate-y-1 hover:shadow-xl">
              <a href="/page/{{id}}" title="{{title}}">
                <img src="{{image.url}}" alt="{{title}}" class="w-full h-56 object-cover rounded-t-xl" />
              </a>
              <div class="p-5">
                <p class="text-xs font-medium uppercase text-red-500 mb-1">{{tag}}</p>
                <h3 class="text-lg font-semibold text-gray-900 mb-3">
                  <a class="transition-colors duration-200 hover:text-blue-600" href="/page/{{id}}">{{title}}</a>
                </h3>
                <div class="flex justify-between items-center">
                  <span class="flex items-center gap-2 text-sm text-gray-500">
                    <svg class="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                        d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"/>
                    </svg>
                    <span data-component="localDateTime">{{publishedAt}}</span>
                  </span>
                  <span class="flex items-center gap-2 text-sm text-gray-500">
                    <svg class="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                    </svg>
                    {{viewCount}}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div class="flex justify-center mt-2 space-x-4" data-component="pagination" >
          <button data-component="previous" class="px-4 py-2 text-xs font-medium text-white bg-indigo-400 border border-gray-200 rounded-lg shadow-sm transition-all duration-200 ease-in-out hover:bg-indigo-500 hover:border-gray-300 hover:shadow-md hover:-translate-y-px disabled:bg-gray-200 disabled:text-gray-400 disabled:border-gray-300 disabled:shadow-none disabled:cursor-not-allowed">Previous</button>
          <button data-component="next" class="px-4 py-2 text-xs font-medium text-white bg-indigo-400 border border-gray-200 rounded-lg shadow-sm transition-all duration-200 ease-in-out hover:bg-indigo-500 hover:border-gray-300 hover:shadow-md hover:-translate-y-px disabled:bg-gray-200 disabled:text-gray-400 disabled:border-gray-300 disabled:shadow-none disabled:cursor-not-allowed">Next</button>
        </div>
      </div>
  `
};