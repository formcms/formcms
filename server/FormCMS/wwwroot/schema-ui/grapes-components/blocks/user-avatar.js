export const userAvatar = {
    category: 'Navigation',
    name: 'user-avatar',
    label: 'User Avatar',
    media: `
      <svg width="54" height="54" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z" fill="#001A72"/>
      </svg>
    `,
    content: `
      <div data-gjs-type="user-avatar" class="relative inline-flex items-center mt-4 md:mt-0 ml-3">
        <button class="focus:outline-none hover:text-gray-900" data-component="user-button">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" viewBox="0 0 24 24" class="w-6 h-6">
                <path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/>
            </svg>
        </button>
      </div>
    `,
};