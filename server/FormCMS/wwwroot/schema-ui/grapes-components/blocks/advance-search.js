export const advancedSearch = {
    category: 'Navigation',
    name: 'advanced-search',
    label: 'Advanced Search',
    media: `
      <svg width="54" height="54" viewBox="0 0 54 54" fill="none" xmlns="http://www.w3.org/2000/svg">
        <circle cx="25" cy="25" r="16" stroke="#001A72" stroke-width="6"/>
        <path d="M35 35L45 45" stroke="#001A72" stroke-width="6" stroke-linecap="round"/>
        <path d="M25 15V35M15 25H35" stroke="#001A72" stroke-width="6" stroke-linecap="round"/>
      </svg>
    `,
    content: `
<div data-gjs-type="advanced-search" class="w-full flex items-center justify-center mt-4 md:mt-0 mr-4">
    <form data-gjs-type="form" method="get" target="_self" action="/search" class="flex flex-col sm:flex-row items-center gap-2">
        <select 
            name="entity" 
            class="w-full sm:w-40 px-3 py-1 rounded-md border border-gray-300 dark:border-gray-600 bg-gray-100 dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:focus:ring-indigo-400"
        >
            <option value="">Select Entity</option>
            <option value="user">User</option>
            <option value="post">Post</option>
            <option value="comment">Comment</option>
        </select>
        <select 
            name="timeframe" 
            class="w-full sm:w-40 px-3 py-1 rounded-md border border-gray-300 dark:border-gray-600 bg-gray-100 dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:focus:ring-indigo-400"
        >
            <option value="">Select Timeframe</option>
            <option value="1day">1 Day</option>
            <option value="1week">1 Week</option>
            <option value="1month">1 Month</option>
            <option value="3month">3 Months</option>
            <option value="1year">1 Year</option>
        </select>
        <input 
            type="text" 
            name="query"
            placeholder="Search ..." 
            class="w-full sm:w-64 px-3 py-1 rounded-md border border-gray-300 dark:border-gray-600 bg-gray-100 dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:focus:ring-indigo-400" 
        />
        <select 
            name="orderby" 
            class="w-full sm:w-40 px-3 py-1 rounded-md border border-gray-300 dark:border-gray-600 bg-gray-100 dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:focus:ring-indigo-400"
        >
            <option value="time">Order by Time</option>
            <option value="score">Order by Match Score</option>
        </select>
        <button 
            type="submit"
            class="w-full sm:w-auto px-3 py-1 bg-indigo-500 dark:bg-indigo-400 text-white rounded-md hover:bg-indigo-600 dark:hover:bg-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:focus:ring-indigo-400"
        >
            Search
        </button>
    </form>
</div>
    `,
};