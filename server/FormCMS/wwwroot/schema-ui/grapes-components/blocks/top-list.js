export const topList = {
    category: 'Data List',
    name: 'top-list',
    label: 'Top List',
    media: `<svg viewBox="0 0 1024 1024" class="icon" xmlns="http://www.w3.org/2000/svg"><path d="M128 128h768v768H128z" fill="#E1F0FF"/><path d="M128 128h768v128H128zM128 256h768v128H128zM128 384h768v128H128zM128 512h768v128H128zM128 640h768v128H128z" fill="#446EB1"/><path d="M128 128h128v640H128z" fill="#6D9EE8"/></svg>`,
    content: `
<div class="mt-8" data-gjs-type="top-list" data-component-type="top-list" data-source="data-list" query="course" qs="sort=count:desc" limit="5">
    {{#each items}}
        <div class="lg:border-t5-100 max-md:border-t5-100">
            <div class="flex">
                <div class="font-source w-10 shrink-0 text-3xl italic text-gray-500">{{add @index 1}}</div>
                <div>
                    <a href="{{url}}" data-title="true" class="block">
                        <h3 class="title16 !font-serifCaption title-extrabold">{{title}}</h3>
                    </a>
                    <div class="flex items-center gap-1 text-xs text-[#737373] mt-1">
                        <button class="group cursor-pointer flex items-center gap-1 min-w-[50px]">
                            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" fill="none" stroke="currentColor" viewBox="2.5 3.5 14 14" class="h-4 w-4">
                                <path d="M9.58464 5.25L10.418 8.16667C9.58464 8.16667 7.91797 8.16667 6.2513 9.83333C4.77816 11.3065 4.16797 13.5833 4.16797 15.6667C5.0013 14.4167 6.12316 13.0544 7.08464 12.3333C8.30547 11.4177 9.58464 11.0833 10.418 11.0833L9.58464 14.4167L15.8346 9.83333L9.58464 5.25Z" stroke-linejoin="round"></path>
                            </svg>
                            <div>{{counts.share}}</div>
                        </button>
                        <a href="{{url}}" class="group cursor-pointer flex items-center gap-1 min-w-[50px]">
                            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="h-4 w-4 !stroke-2">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M12 20.25c4.97 0 9-3.694 9-8.25s-4.03-8.25-9-8.25S3 7.444 3 12c0 2.104.859 4.023 2.273 5.48.432.447.74 1.04.586 1.641a4.483 4.483 0 01-.923 1.785A5.969 5.969 0 006 21c1.282 0 2.47-.402 3.445-1.087.81.22 1.668.337 2.555.337z"></path>
                            </svg>
                            <div>{{counts.like}}</div>
                        </a>
                    </div>
                </div>
            </div>
        </div>
    {{/each}}
</div>
    `
};