export const carousel = {
    category: 'Data Display',
    name: 'carousel',
    label: 'Carousel',
    media: `
      <svg width="54" height="54" viewBox="0 0 54 54" fill="none" xmlns="http://www.w3.org/2000/svg">
        <rect x="2" y="4" width="50" height="46" rx="3" fill="#E1F0FF"/>
        <rect x="8" y="10" width="38" height="34" rx="2" fill="#6D9EE8"/>
        <path d="M8 27L14 19V35L8 27Z" fill="#446EB1"/>
        <path d="M46 27L40 19V35L46 27Z" fill="#446EB1"/>
      </svg>
    `,
    content: `
<div data-gjs-type="data-list"  data-component="data-list"  class="py-1">
    <div data-gjs-type="foreach" data-component="foreach" class="carousel w-full">
      <div id="{{@index}}" class="carousel-item w-full">
        <img alt="" src="{{url}}" class="w-full min-h-[100px]" />
      </div>
    </div>
    <div data-gjs-type="foreach" data-component="foreach" class="flex w-full justify-center gap-2 py-2">
      <a href="#{{@index}}" class="btn btn-xs">{{@index}}</a>
    </div>
</div>
    `,
};