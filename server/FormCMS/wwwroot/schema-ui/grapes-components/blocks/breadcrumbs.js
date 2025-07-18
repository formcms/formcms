export const breadcrumbs = {
    category: 'Navigation',
    name: 'breadcrumbs',
    label: 'Breadcrumbs',
    media: `
      <svg width="54" height="54" viewBox="0 0 54 54" fill="none" xmlns="http://www.w3.org/2000/svg">
        <rect x="2" y="4" width="14" height="46" rx="2" fill="var(--breadcrumb-bg-1, #E1F0FF)"/>
        <rect x="20" y="4" width="14" height="46" rx="2" fill="var(--breadcrumb-bg-2, #6D9EE8)"/>
        <rect x="38" y="4" width="14" height="46" rx="2" fill="var(--breadcrumb-bg-3, #446EB1)"/>
        <path d="M16 4L18 4L18 50L16 50Z" fill="var(--breadcrumb-divider, #FFFFFF)"/>
        <path d="M34 4L36 4L36 50L34 50Z" fill="var(--breadcrumb-divider, #FFFFFF)"/>
      </svg>
    `,
    content: `
<div class="breadcrumbs text-sm" data-gjs-type="data-list" data-component="data-list">
  <ul data-gjs-type="foreach" data-component="foreach">
    <li><a href="/page/{{id}}">{{name}}</a></li>
  </ul>
</div>
<style>
.breadcrumbs {
  --breadcrumb-bg: #f8fafc;
  --breadcrumb-text: #1f2937;
  --breadcrumb-link: #2563eb;
  --breadcrumb-link-hover: #1e40af;
  --breadcrumb-divider: #9ca3af;
}

@media (prefers-color-scheme: dark) {
  .breadcrumbs {
    --breadcrumb-bg: #1f2937;
    --breadcrumb-text: #e5e7eb;
    --breadcrumb-link: #60a5fa;
    --breadcrumb-link-hover: #93c5fd;
    --breadcrumb-divider: #4b5563;
  }
}

.breadcrumbs ul {
  display: flex;
  list-style: none;
  padding: 0;
  margin: 0;
  background: var(--breadcrumb-bg);
  color: var(--breadcrumb-text);
}

.breadcrumbs li {
  display: flex;
  align-items: center;
}

.breadcrumbs li:not(:last-child)::after {
  content: '/';
  margin: 0 0.5rem;
  color: var(--breadcrumb-divider);
}

.breadcrumbs a {
  color: var(--breadcrumb-link);
  text-decoration: none;
}

.breadcrumbs a:hover {
  color: var(--breadcrumb-link-hover);
  text-decoration: underline;
}

/* SVG icon dark mode colors */
:root {
  --breadcrumb-bg-1: #E1F0FF;
  --breadcrumb-bg-2: #6D9EE8;
  --breadcrumb-bg-3: #446EB1;
  --breadcrumb-divider: #FFFFFF;
}

@media (prefers-color-scheme: dark) {
  :root {
    --breadcrumb-bg-1: #4b5563;
    --breadcrumb-bg-2: #6b7280;
    --breadcrumb-bg-3: #9ca3af;
    --breadcrumb-divider: #d1d5db;
  }
}
</style>
    `,
};