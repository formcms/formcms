export const featuredList = {
    category: 'Data List',
    name: 'featured-list',
    label: 'Featured List',
    media: `
    <svg viewBox="0 0 1024 1024" class="icon" xmlns="http://www.w3.org/2000/svg">
        <rect x="64" y="64" width="896" height="896" fill="#E1F0FF"/>
        <rect x="64" y="128" width="128" height="128" fill="#6D9EE8"/>
        <rect x="256" y="128" width="640" height="128" fill="#446EB1"/>
        <rect x="64" y="320" width="128" height="128" fill="#6D9EE8"/>
        <rect x="256" y="320" width="640" height="128" fill="#446EB1"/>
        <rect x="64" y="512" width="128" height="128" fill="#6D9EE8"/>
        <rect x="256" y="512" width="640" height="128" fill="#446EB1"/>
        <rect x="64" y="704" width="128" height="128" fill="#6D9EE8"/>
        <rect x="256" y="704" width="640" height="128" fill="#446EB1"/>
    </svg>
`,
    content: `
<div class="py-6" data-gjs-type="data-list" data-component="data-list" offset="0" limit="5">
    <h3 class="sm:text-2xl text-2xl font-bold title-font mb-2 text-gray-900">Related List</h3>
    <div class="mt-4" data-gjs-type="foreach" data-component="foreach">
        <div class="lg:border-t5-100 max-md:border-t5-100 bg-white p-3 rounded-lg shadow-sm hover:shadow-md transition-shadow duration-200 mb-2">
            <div class="flex items-start">
                <div class="w-16 h-16 shrink-0 mr-3">
                    <a href="/page/{{id}}" data-title="true" class="block">
                    <img src="{{image.url}}" alt="{{title}}" class="w-full h-full object-cover rounded-md" />
                    </a>
                </div>
                <div class="flex-1">
                    <a href="/page/{{id}}" data-title="true" class="block">
                        <h3 class="text-lg font-serif font-extrabold text-gray-800 hover:text-blue-600 transition-colors duration-200">{{title}}</h3>
                    </a>
                    <div class="flex items-center gap-2 text-xs text-gray-500 mt-2">
                        <button class="group cursor-pointer flex items-center gap-1 min-w-[50px] hover:text-blue-500 transition-colors duration-200">
                            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" fill="none" stroke="currentColor" viewBox="2.5 3.5 14 14" class="h-4 w-4">
                                <path d="M9.58464 5.25L10.418 8.16667C9.58464 8.16667 7.91797 8.16667 6.2513 9.83333C4.77816 11.3065 4.16797 13.5833 4.16797 15.6667C5.0013 14.4167 6.12316 13.0544 7.08464 12.3333C8.30547 11.4177 9.58464 11.0833 10.418 11.0833L9.58464 14.4167L15.8346 9.83333L9.58464 5.25Z" stroke-linejoin="round"></path>
                            </svg>
                            <div>{{counts.share}}</div>
                        </button>
                        <button class="group cursor-pointer flex items-center gap-1 min-w-[50px] hover:text-blue-500 transition-colors duration-200">
                            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="h-4 w-4 !stroke-2">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
                            </svg>
                            <div>{{counts.like}}</div>
                        </button>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>
`
};