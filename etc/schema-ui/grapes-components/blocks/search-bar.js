export const searchBar = {
    category: 'Navigation',
    name: 'search-bar',
    label: 'Search Bar',
    media: `
      <svg width="54" height="54" viewBox="0 0 54 54" fill="none" xmlns="http://www.w3.org/2000/svg">
        <circle cx="25" cy="25" r="16" stroke="#001A72" stroke-width="6"/>
        <path d="M35 35L45 45" stroke="#001A72" stroke-width="6" stroke-linecap="round"/>
      </svg>
    `,
    content: `
<div data-gjs-type="search-bar" class="w-full flex items-center justify-center mt-4 md:mt-0 mr-4">
    <form data-gjs-type="form" method="get" target="_self" action="/search" class="flex items-center">
        <input 
            type="text" 
            name="query"
            placeholder="Search ..." 
            class="w-64 px-3 py-1 rounded-l-md border border-gray-300 dark:border-gray-600 bg-gray-100 dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:focus:ring-indigo-400" 
        />
        <button 
            type="submit"
            class="px-3 py-1 bg-indigo-500 dark:bg-indigo-400 text-white rounded-r-md hover:bg-indigo-600 dark:hover:bg-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:focus:ring-indigo-400"
        >
            Search
        </button>
        <a 
            href="#" 
            class="ml-2 px-3 py-1 bg-gray-100 dark:bg-gray-600 text-gray-900 dark:text-gray-100 rounded-md hover:bg-gray-200 dark:hover:bg-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:focus:ring-indigo-400"
        >
            Advanced Search
        </a>
    </form>
</div>
    `,
};