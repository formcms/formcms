export const headerB = {
    category: 'Navigation',
    name: 'header-b',
    label: 'Header B',
    media: `
      <svg width="54" height="54" viewBox="0 0 54 54" fill="none" xmlns="http://www.w3.org/2000/svg">
        <rect x="2" y="6" width="50" height="8" rx="2" fill="#001A72"/>
        <rect x="2" y="22" width="50" height="8" rx="2" fill="#001A72"/>
        <rect x="2" y="38" width="36" height="8" rx="2" fill="#001A72"/>
        <path d="M42 50L52 27L42 4" stroke="#001A72" stroke-width="6" stroke-linecap="round" stroke-linejoin="round"/>
      </svg>
    `,
    content: `
  <div data-gjs-type="header-b" class="container mx-auto flex flex-wrap p-5 flex-col md:flex-row items-center text-gray-600 dark:text-gray-300 body-font bg-white dark:bg-gray-800">
    <a href="/" class="flex title-font font-medium items-center text-gray-900 dark:text-gray-100 mb-4 md:mb-0">
     <svg xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" class="w-10 h-10 text-white p-2 bg-indigo-500 dark:bg-indigo-400 rounded-full" viewBox="0 0 24 24">
         <path d="M3 12l9-9 9 9M9 21V12h6v9M4 12h16v8H4z" />
         <path d="M8 21h8M10 16h4v5h-4z" />
    </svg>
      <span class="ml-3 text-xl">HomePage</span>
    </a>
    <nav class="md:mr-auto md:ml-4 md:py-1 md:pl-4 md:border-l md:border-gray-400 dark:md:border-gray-600 flex flex-wrap items-center text-base justify-center">
      <a class="mr-5 hover:text-gray-900 dark:hover:text-gray-100">First Link</a>
      <a class="mr-5 hover:text-gray-900 dark:hover:text-gray-100">Second Link</a>
      <a class="mr-5 hover:text-gray-900 dark:hover:text-gray-100">Third Link</a>
      <a class="mr-5 hover:text-gray-900 dark:hover:text-gray-100">Fourth Link</a>
    </nav>
     <a href="/portal" class="inline-flex items-center bg-gray-100 dark:bg-gray-600 border-0 py-1 px-3 focus:outline-none hover:bg-gray-200 dark:hover:bg-gray-500 rounded text-base mt-4 md:mt-0">User Portal
      <svg fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" class="w-4 h-4 ml-1 text-gray-600 dark:text-gray-300" viewBox="0 0 24 24">
        <path d="M5 12h14M12 5l7 7-7 7"></path>
      </svg>
    </a>
    <a href="/admin" class="inline-flex items-center bg-gray-100 dark:bg-gray-600 border-0 py-1 px-3 focus:outline-none hover:bg-gray-200 dark:hover:bg-gray-500 rounded text-base mt-4 md:mt-0">Admin Panel
      <svg fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" class="w-4 h-4 ml-1 text-gray-600 dark:text-gray-300" viewBox="0 0 24 24">
        <path d="M5 12h14M12 5l7 7-7 7"></path>
      </svg>
    </a>
     <a href="/schema" class="inline-flex items-center bg-gray-100 dark:bg-gray-600 border-0 py-1 px-3 focus:outline-none hover:bg-gray-200 dark:hover:bg-gray-500 rounded text-base mt-4 md:mt-0">Schema Builder
      <svg fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" class="w-4 h-4 ml-1 text-gray-600 dark:text-gray-300" viewBox="0 0 24 24">
        <path d="M5 12h14M12 5l7 7-7 7"></path>
      </svg>
    </a>
  </div>
    `,
};