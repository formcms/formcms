export const cardA =
    {
        category:'Data Display',
        name: 'card-a',
        label: 'Card A',
        media: `<svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink">
  <defs>
    <polygon id="card-a" points="0 .069 0 3 18 3 18 0"/>
    <path id="card-c" d="M18,5 L18,2 L2,2 L2,5 L18,5 Z M18,7 L2,7 L2,13 L18,13 L18,7 Z M2,0 L18,0 C19.1045695,0 20,0.8954305 20,2 L20,13 C20,14.1045695 19.1045695,15 18,15 L2,15 C0.8954305,15 0,14.1045695 0,13 L0,2 C0,0.8954305 0.8954305,0 2,0 Z M13,9 L15,9 C15.5522847,9 16,9.44771525 16,10 C16,10.5522847 15.5522847,11 15,11 L13,11 C12.4477153,11 12,10.5522847 12,10 C12,9.44771525 12.4477153,9 13,9 Z"/>
  </defs>
  <g fill="none" fill-rule="evenodd" transform="translate(2 5)">
    <g transform="translate(1 3)">
      <mask id="card-b" fill="#ffffff">
        <use xlink:href="#card-a"/>
      </mask>
      <use fill="#D8D8D8" xlink:href="#card-a"/>
      <g fill="#FFA0A0" mask="url(#card-b)">
        <rect width="24" height="24" transform="translate(-3 -8)"/>
      </g>
    </g>
    <mask id="card-d" fill="#ffffff">
      <use xlink:href="#card-c"/>
    </mask>
    <use fill="#000000" fill-rule="nonzero" xlink:href="#card-c"/>
    <g fill="#7600FF" mask="url(#card-d)">
      <rect width="24" height="24" transform="translate(-2 -5)"/>
    </g>
  </g>
</svg>`,
        content: `
<div class="container px-8 mx-auto xl:px-5  max-w-screen-lg py-5 lg:py-8 !pt-0" data-gjs-type="card-a">
  <div class="mx-auto max-w-screen-md ">
    <div class="mt-3 rounded-2xl bg-gray-50 px-8 py-8 text-gray-500 dark:bg-gray-900 dark:text-gray-400">
      <div class="flex flex-wrap items-start sm:flex-nowrap sm:space-x-6">
        <div class="relative mt-1 h-24 w-24 flex-shrink-0 ">
            <a href="pages/{{page}}"> <img loading="lazy" decoding="async" data-nimg="fill" class="rounded-full object-cover" style="position:absolute;height:100%;width:100%;left:0;top:0;right:0;bottom:0;color:transparent" src="/files/{{image}}"> </a>
        </div>
        <div>
            <div class="mb-3"> <h3 class="text-lg font-medium text-gray-800 dark:text-gray-300">{{title}}</h3> </div>
            <div>{{{content}}}</div>
        </div>
      </div>
    </div>        
  </div>
</div>
 `
    }
