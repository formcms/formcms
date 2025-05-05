export const ecommerceA = {
        category: 'Data List',
        name: 'ecommerce-a',
        label: `Ecommerce A`,
        media: `<svg viewBox="-0.5 0 19 19" version="1.1" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" xmlns:sketch="http://www.bohemiancoding.com/sketch/ns">
        <title>icon/18/icon-grid-16</title>
        <desc>Created with Sketch.</desc>
        <defs></defs>
        <g id="out" stroke="none" stroke-width="1" fill="none" fill-rule="evenodd" sketch:type="MSPage">
            <path d="M1,9.88888892 L4.66666663,9.88888892 L4.66666663,13.5555555 L1,13.5555555 L1,9.88888892 L1,9.88888892 Z M5.44444446,9.88888892 L9.11111108,9.88888892 L9.11111108,13.5555555 L5.44444446,13.5555555 L5.44444446,9.88888892 L5.44444446,9.88888892 Z M9.88888892,9.88888892 L13.5555555,9.88888892 L13.5555555,13.5555555 L9.88888892,13.5555555 L9.88888892,9.88888892 L9.88888892,9.88888892 Z M14.3333334,9.88888892 L18,9.88888892 L18,13.5555555 L14.3333334,13.5555555 L14.3333334,9.88888892 L14.3333334,9.88888892 Z M1,14.3333334 L4.66666663,14.3333334 L4.66666663,18 L1,18 L1,14.3333334 L1,14.3333334 Z M5.44444446,14.3333334 L9.11111108,14.3333334 L9.11111108,18 L5.44444446,18 L5.44444446,14.3333334 L5.44444446,14.3333334 Z M9.88888892,14.3333334 L13.5555555,14.3333334 L13.5555555,18 L9.88888892,18 L9.88888892,14.3333334 L9.88888892,14.3333334 Z M14.3333334,14.3333334 L18,14.3333334 L18,18 L14.3333334,18 L14.3333334,14.3333334 L14.3333334,14.3333334 Z M1,5.44444446 L4.66666663,5.44444446 L4.66666663,9.11111108 L1,9.11111108 L1,5.44444446 L1,5.44444446 Z M5.44444446,5.44444446 L9.11111108,5.44444446 L9.11111108,9.11111108 L5.44444446,9.11111108 L5.44444446,5.44444446 L5.44444446,5.44444446 Z M9.88888892,5.44444446 L13.5555555,5.44444446 L13.5555555,9.11111108 L9.88888892,9.11111108 L9.88888892,5.44444446 L9.88888892,5.44444446 Z M14.3333334,5.44444446 L18,5.44444446 L18,9.11111108 L14.3333334,9.11111108 L14.3333334,5.44444446 L14.3333334,5.44444446 Z M1,1 L4.66666663,1 L4.66666663,4.66666663 L1,4.66666663 L1,1 L1,1 Z M5.44444446,1 L9.11111108,1 L9.11111108,4.66666663 L5.44444446,4.66666663 L5.44444446,1 L5.44444446,1 Z M9.88888892,1 L13.5555555,1 L13.5555555,4.66666663 L9.88888892,4.66666663 L9.88888892,1 L9.88888892,1 Z M14.3333334,1 L18,1 L18,4.66666663 L14.3333334,4.66666663 L14.3333334,1 L14.3333334,1 Z" id="path" fill="#000000" sketch:type="MSShapeGroup"></path>
        </g>
    </svg>`,
        content: `
<style>
    /* Card and hover effects */
    .item-card {
        transition: transform 0.3s ease, box-shadow 0.3s ease;
        background: #ffffff;
        border-radius: 8px;
        overflow: hidden;
    }
    .item-card:hover {
        transform: translateY(-5px);
        box-shadow: 0 10px 15px rgba(0, 0, 0, 0.1);
    }
    /* Image styling */
    .item-image {
        transition: opacity 0.3s ease;
        height: 200px;
        width: 100%;
        object-fit: cover;
        border-radius: 8px 8px 0 0;
    }
    .item-image:hover {
        opacity: 0.9;
    }
    /* Pagination buttons */
    .pagination-btn {
        transition: background-color 0.3s ease, transform 0.2s ease;
        background: linear-gradient(to right, #a78bfa, #60a5fa);
        color: #ffffff;
        border: none;
        padding: 10px 20px;
        border-radius: 9999px;
        font-weight: 600;
    }
    .pagination-btn:hover {
        transform: scale(1.05);
        background: linear-gradient(to right, #8b5cf6, #3b82f6);
    }
    .pagination-btn:disabled {
        background: #e5e7eb;
        color: #6b7280;
        cursor: not-allowed;
    }
    /* Meta info styling */
    .meta-info {
        display: flex;
        align-items: center;
        gap: 8px;
        font-size: 0.875rem;
        color: #6b7280; /* gray-500 */
    }
    .meta-icon {
        width: 16px;
        height: 16px;
        color: #9ca3af; /* gray-400 */
    }
    .category {
        text-transform: uppercase;
        font-weight: 500;
        color: #ef4444; /* red-500 */
    }
    .title {
        transition: color 0.2s ease;
    }
    .title:hover {
        color: #2563eb; /* blue-600 */
    }
</style>
<section class="text-gray-600 body-font">
  <div class="container px-5 py-12 mx-auto">
    <div class="flex flex-wrap w-full mb-8">
      <div class="lg:w-1/2 w-full mb-6 lg:mb-0">
        <h1 class="sm:text-4xl text-3xl font-bold title-font mb-2 text-gray-900">Our Products</h1>
        <div class="h-1 w-20 bg-red-500 rounded"></div>
      </div>
        <p class="lg:w-1/2 w-full leading-relaxed text-gray-500 prose">
        Discover our curated selection of products, powered by <a href="https://github.com/FormCMS/FormCMS/" class="text-blue-500 hover:text-blue-700">FormCMS</a>. Browse with ease using our intuitive pagination controls.
      </p>
    </div>
    <div class="flex flex-wrap -m-4" data-gjs-type="data-list" data-source="data-list">
      <div class="lg:w-1/4 md:w-1/2 p-4 w-full">
        <div class="item-card">
          <a href="/pageName/{{slug}}" title="{{title}}">
            <img src="{{image.url}}" alt="" class="h-40 rounded w-full object-cover object-center mb-6 item-image">
          </a>
          <div class="p-6">
            <h3 class="category text-xs tracking-widest mb-2">{{category}}</h3>
            <h2 class="text-lg text-gray-900 font-medium title-font mb-3">
              <a href="/pageName/{{slug}}" class="title">{{title}}</a>
            </h2>
            <div class="mt-4 flex justify-between">
              <span class="meta-info">
                <svg class="meta-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"></path></svg>
                <span data-component="localDateTime">{{publishedAt}}</span>
              </span>
              <span class="meta-info">
                <svg class="meta-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0zm6 0c0 5.523-4.477 10-10 10S1 17.523 1 12 5.477 2 11 2s10 4.477 10 10z"></path></svg>
                {{viewCount}}
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
    <nav aria-label="Pagination" class="flex justify-center space-x-3 mt-8">
        <a data-command="previous" class="relative inline-flex items-center px-5 py-2 text-sm bg-gradient-to-r from-violet-300 to-indigo-300 border border-fuchsia-100 hover:border-violet-100 font-semibold cursor-pointer leading-5 rounded-full transition duration-150 ease-in-out focus:outline-none focus:shadow-outline-blue focus:border-blue-300 pagination-btn" style="display: none;">Previous</a>
        <a data-command="next" class="relative inline-flex items-center px-5 py-2 text-sm bg-gradient-to-r from-violet-300 to-indigo-300 border border-fuchsia-100 hover:border-violet-100 font-semibold cursor-pointer leading-5 rounded-full transition duration-150 ease-in-out focus:outline-none focus:shadow-outline-blue focus:border-blue-300 pagination-btn">Next</a>
    </nav>
  </div>
</section>
    `
};