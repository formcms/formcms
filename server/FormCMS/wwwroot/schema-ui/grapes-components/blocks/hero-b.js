export const heroB = {
        category: 'Data Display',
        name: 'hero-b',
        label: 'Hero B',
        media: `<svg fill="#000000" id="Layer_1" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 190" xml:space="preserve">
<path d="M48.12,27.903C48.12,13.564,59.592,2,74.023,2c14.339,0,25.903,11.564,25.903,25.903
\tC99.834,42.335,88.27,53.806,74.023,53.806C59.684,53.806,48.12,42.242,48.12,27.903z M191,139h-47V97c0-20.461-17.881-37-38-37H43
\tC20.912,60,1.99,79.14,2,98v77c-0.026,8.533,6.001,12.989,12,13c6.014,0.011,12-4.445,12-13v-75h8v88h78v-88h8l0.081,50.37
\tc-0.053,8.729,5.342,12.446,10.919,12.63h60C207.363,163,207.363,139,191,139z M229.928,67.704
\tC243.948,62.648,254,49.213,254,33.471l-0.086-3.846h-22.129v-9.958h-59.109v9.958h-22.129l-0.086,3.846
\tc0,15.742,10.052,29.176,24.072,34.233c3.302,8.327,10.305,14.384,18.93,17.054v9.04c-0.388,7.082-6.379,13.109-13.561,13.109v8.136
\th-7.232v15.821h59.121v-15.821h-7.232v-8.136c-7.182,0-13.172-6.026-13.561-13.109v-9.04
\tC219.623,82.088,226.625,76.03,229.928,67.704z M231.791,37.296h13.815c-0.027,0.193-0.047,0.46-0.077,0.651
\tc-1.365,8.523-6.574,15.677-13.744,19.892C231.803,57.415,231.791,37.296,231.791,37.296z M158.932,37.947
\tc-0.031-0.192-0.05-0.458-0.077-0.651h13.815c0,0-0.012,20.119,0.006,20.544C165.506,53.624,160.298,46.47,158.932,37.947z
\t M202.231,56.632l-9.156,6.111l2.983-10.596l-8.641-6.819l11-0.437l3.815-10.326l3.815,10.326l11,0.437l-8.642,6.819l2.983,10.596
\tL202.231,56.632z"/>
</svg>`,
        content: `
<div data-gjs-type='hero-b' class="container px-8 mx-auto xl:px-5 max-w-screen-lg py-5 lg:py-8 !pt-0 bg-white dark:bg-gray-800">
  <div class="mx-auto max-w-screen-md">
    <div class="flex justify-center">
      <div class="flex gap-3">
        <span class="inline-block text-xs font-medium tracking-wider mt-5 text-pink-600 dark:text-pink-400">{{tag}}</span>
      </div>
    </div>
    <h1 class="text-brand-primary mb-3 mt-2 text-center text-3xl font-semibold tracking-tight text-gray-900 dark:text-gray-100 lg:text-4xl lg:leading-snug">{{title}}</h1>
    <div class="mt-3 flex justify-center space-x-3 text-gray-500 dark:text-gray-400">
      <div class="flex items-center gap-3">
        <div class="relative h-10 w-10 flex-shrink-0">
          <img alt="Li Bai" loading="lazy" decoding="async" data-nimg="fill" class="rounded-full object-cover" src="/files{{author.image}}" style="position: absolute; height: 100%; width: 100%; inset: 0px; color: transparent;"/>
        </div>
        <div class="flex items-center space-x-2 text-sm">
          <p class="text-gray-800 dark:text-gray-200">
            <a href="/page/{{author.id}}" class="hover:text-blue-600 dark:hover:text-blue-400">{{author.name}}</a>
          </p>
        </div>
        <div>
          <div class="flex items-center space-x-2 text-sm">
            <time class="text-gray-500 dark:text-gray-400">{{date}}</time>
            <span class="text-gray-500 dark:text-gray-400">{{time_to_read}}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</div>
<div class="relative z-0 mx-auto aspect-video max-w-screen-lg overflow-hidden lg:rounded-lg">
  <img alt="Thumbnail" loading="eager" decoding="async" data-nimg="fill" class="object-cover" src="/files{{image}}"/>
</div>
<div class="container px-8 mx-auto xl:px-5 max-w-screen-lg py-5 lg:py-8 bg-white dark:bg-gray-800">
  <article class="mx-auto max-w-screen-md">
    <div class="prose mx-auto my-3 dark:prose-invert prose-a:text-blue-600 dark:prose-a:text-blue-400">{{{content}}}</div>
  </article>
</div>
    `,
};