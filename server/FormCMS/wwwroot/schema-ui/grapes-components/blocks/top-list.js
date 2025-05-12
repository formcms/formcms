export const topList = {
    category: 'Data List',
    name: 'top-list',
    label: 'Top List',
    media: `<svg viewBox="0 0 1024 1024" class="icon" xmlns="http://www.w3.org/2000/svg"><path d="M128 128h768v768H128z" fill="#E1F0FF"/><path d="M128 128h768v128H128zM128 256h768v128H128zM128 384h768v128H128zM128 512h768v128H128zM128 640h768v128H128z" fill="#446EB1"/><path d="M128 128h128v640H128z" fill="#6D9EE8"/></svg>`,
    content: `
<div class="py-6" data-gjs-type="top-list"  data-component="top-list" offset="0" limit="5">
    <h3 class="sm:text-2xl text-2xl font-bold title-font mb-2 text-gray-900">Top List</h3>
    <div class="mt-4"  data-gjs-type="foreach" data-component="foreach">
        <div class="lg:border-t5-100 max-md:border-t5-100 bg-white p-3 rounded-lg shadow-sm hover:shadow-md transition-shadow duration-200 mb-2">
            <div class="flex items-start">
                <div class="font-source w-10 shrink-0 text-3xl italic text-gray-500">{{i}}</div>
                <div class="flex-1">
                    <a href="{{url}}" data-title="true" class="block">
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