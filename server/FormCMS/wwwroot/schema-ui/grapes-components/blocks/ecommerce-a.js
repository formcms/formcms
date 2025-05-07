export const ecommerceA = {
        category: 'Data List',
        name: 'ecommerce-a',
        label: 'Ecommerce A',
        media: `<svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
  <path d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2 4h13" stroke="#000" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
  <circle cx="9" cy="21" r="1" fill="#000"/>
  <circle cx="20" cy="21" r="1" fill="#000"/>
</svg>`,
        content: `
    <style>
      .item-card {
        transition: transform 0.3s ease, box-shadow 0.3s ease;
        background: #fff;
        border-radius: 0.75rem;
        overflow: hidden;
        box-shadow: 0 0 0 1px #e5e7eb;
      }

      .item-card:hover {
        transform: translateY(-4px);
        box-shadow: 0 12px 20px rgba(0, 0, 0, 0.07);
      }

      .item-image {
        object-fit: cover;
        height: 220px;
        width: 100%;
        border-radius: 0.75rem 0.75rem 0 0;
      }

      .pagination-btn {
        transition: all 0.2s ease-in-out;
        padding: 10px 20px;
        font-weight: 600;
        border-radius: 9999px;
        background: linear-gradient(to right, #c084fc, #818cf8);
        color: white;
        border: 2px solid #ffffff; /* Add this line for a white border */
      }

      .pagination-btn:hover {
        transform: scale(1.05);
        background: linear-gradient(to right, #a855f7, #6366f1);
      }

      .pagination-btn:disabled {
        background: #e5e7eb;
        color: #9ca3af;
        cursor: not-allowed;
      }

      .meta-info {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        font-size: 0.875rem;
        color: #6b7280;
      }

      .meta-icon {
        width: 16px;
        height: 16px;
        color: #9ca3af;
      }

      .category {
        text-transform: uppercase;
        font-size: 0.75rem;
        font-weight: 500;
        color: #ef4444;
      }

      .title {
        transition: color 0.2s ease-in-out;
      }

      .title:hover {
        color: #2563eb;
      }
    </style>

    <section class="text-gray-700 body-font">
      <div class="container px-5 py-5 mx-auto">
        <h2 class="text-3xl sm:text-4xl font-bold mb-8 text-gray-900">Ecommerce A</h2>
        <div class="flex flex-wrap -m-4" data-gjs-type="data-list" data-source="data-list" limit="4" offset="0">
          <div class="p-4 md:w-1/2 lg:w-1/4 w-full">
            <div class="item-card">
              <a href="/pageName/{{slug}}" title="{{title}}">
                <img src="{{image.url}}" alt="{{title}}" class="w-full h-56 object-cover rounded-t-xl" />
              </a>
              <div class="p-5">
                <p class="category mb-1">{{category}}</p>
                <h3 class="text-lg font-semibold text-gray-900 mb-3">
                  <a class="title" href="/pageName/{{slug}}">{{title}}</a>
                </h3>
                <div class="flex justify-between items-center">
                  <span class="meta-info">
                    <svg class="meta-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                        d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"/>
                    </svg>
                    <span data-component="localDateTime">{{publishedAt}}</span>
                  </span>
                  <span class="meta-info">
                    <svg class="meta-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                        d="M15 12a3 3 0 11-6 0 3 3 0 016 0zm6 0c0 5.523-4.477 10-10 10S1 17.523 1 12 5.477 2 11 2s10 4.477 10 10z"/>
                    </svg>
                    {{viewCount}}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="flex justify-center mt-2 space-x-4">
          <button data-command="previous" class="pagination-btn" style="display: none;">Previous</button>
          <button data-command="next" class="pagination-btn">Next</button>
        </div>
      </div>
    </section>
  `
};
