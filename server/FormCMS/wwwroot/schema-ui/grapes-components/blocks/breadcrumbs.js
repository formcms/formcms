export const breadcrumbs = {
    category: 'Navigation',
    name: 'breadcrumbs',
    label: 'Breadcrumbs',
    media: `
      <svg width="54" height="54" viewBox="0 0 54 54" fill="none" xmlns="http://www.w3.org/2000/svg">
        <rect x="2" y="4" width="14" height="46" rx="2" fill="#E1F0FF"/>
        <rect x="20" y="4" width="14" height="46" rx="2" fill="#6D9EE8"/>
        <rect x="38" y="4" width="14" height="46" rx="2" fill="#446EB1"/>
        <path d="M16 4L18 4L18 50L16 50Z" fill="#FFFFFF"/>
        <path d="M34 4L36 4L36 50L34 50Z" fill="#FFFFFF"/>
      </svg>
    `,
    content: `
<div class="breadcrumbs text-sm" data-gjs-type="data-list" data-component="data-list">
  <ul data-gjs-type="foreach" data-component="foreach">
    <li><a href="/page/{{id}}">{{name}}</a></li>
  </ul>
</div>
    `,
};