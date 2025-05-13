/*
data-gjs-type: for grapes.js attach trait
data-component-type: for back end to prepare data
* */

export const listA =
    {
        category:'Data List',
        name: 'list-a',
        label: 'List A',
        media: `<svg viewBox="0 0 1024 1024" class="icon"  xmlns="http://www.w3.org/2000/svg"><path d="M214.5 264.6h587.4v513.9H214.5z" fill="#E1F0FF" /><path d="M214.5 240.2h587.4v97.9H214.5zM801.9 240.2h24.5v538.4h-24.5zM190 240.2h24.5v538.4H190z" fill="#446EB1" /><path d="M214.5 460.4h587.4v24.5H214.5zM214.5 607.3h587.4v24.5H214.5z" fill="#6D9EE8" /><path d="M385.8 338.1h24.5v416h-24.5zM606.1 338.1h24.5v416h-24.5z" fill="#6D9EE8" /><path d="M214.5 754.1h587.4v24.5H214.5z" fill="#446EB1" /></svg>`,
        content: `
    <div class="px-4 sm:px-8 max-w-5xl m-auto"  data-gjs-type="data-list"  data-component="data-list">
        <h1 class="text-center font-semibold text-sm">List Group</h1>
        <ul class="border border-gray-200 rounded overflow-hidden shadow-md p-1"  data-gjs-type="foreach" data-component="foreach">
            <li class="px-4 py-2 bg-white hover:bg-sky-100 hover:text-sky-900 border-b last:border-none border-gray-200 transition-all duration-300 ease-in-out">{{title}}</li>
        </ul>
        <nav class="flex space-x-2" aria-label="Pagination">
            <a data-component="previous" class="relative inline-flex items-center px-4 py-2 text-sm bg-gradient-to-r from-violet-300 to-indigo-300 border border-fuchsia-100 hover:border-violet-100 font-semibold cursor-pointer leading-5 rounded-md transition duration-150 ease-in-out focus:outline-none focus:shadow-outline-blue focus:border-blue-300 focus:z-10"> Previous </a>
            <a data-component="next" class="relative inline-flex items-center px-4 py-2 text-sm bg-gradient-to-r from-violet-300 to-indigo-300 border border-fuchsia-100 hover:border-violet-100 font-semibold cursor-pointer leading-5 rounded-md transition duration-150 ease-in-out focus:outline-none focus:shadow-outline-blue focus:border-blue-300 focus:z-10"> Next </a>
        </nav>
    </div>
 `
    }