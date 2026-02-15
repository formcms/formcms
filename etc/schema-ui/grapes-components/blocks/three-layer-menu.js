export const threeLayerMenu = {
    category: 'Navigation',
    name: 'three-level-menu',
    label: 'Three level menu',
    media: `
<svg xmlns="http://www.w3.org/2000/svg" class="w-6 h-6 text-blue-600 dark:text-blue-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h12M4 18h8"/>
</svg>
  `,
    content: `
<div data-gjs-type="data-list" data-component="data-list" class="menu bg-white dark:bg-gray-800 shadow-lg rounded-xl w-64 p-4 space-y-2 border border-gray-200 dark:border-gray-700">
    <ul data-gjs-type="foreach" data-component="foreach">
      <li>
        <details open class="group">
          <summary class="cursor-pointer text-base font-semibold text-gray-800 dark:text-gray-200 hover:text-blue-600 dark:hover:text-blue-400">
            <a href="/page/{{id}}">{{name}}</a>
          </summary>
          <ul class="pl-4 space-y-2 mt-2 border-l border-gray-200 dark:border-gray-600">
            {{#each children}}
            <li>
              <details open class="group">
                <summary class="cursor-pointer text-sm font-medium text-gray-700 dark:text-gray-300 hover:text-blue-500 dark:hover:text-blue-300">
                  <a href="/page/{{id}}">{{name}}</a>
                </summary>
                <ul class="pl-4 space-y-1 mt-1 border-l border-gray-100 dark:border-gray-500">
                  {{#each children}}
                  <li>
                    <a href="/page/{{id}}" class="text-sm text-gray-600 dark:text-gray-400 hover:text-blue-400 dark:hover:text-blue-200 hover:underline">{{name}}</a>
                  </li>
                  {{/each}}
                </ul>
              </details>
            </li>
            {{/each}}
          </ul>
        </details>
      </li>
    </ul>
</div>
`
}