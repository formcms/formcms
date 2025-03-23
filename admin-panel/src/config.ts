export const configs = {
    apiURL: import.meta.env.VITE_REACT_APP_API_URL,
    entityRouterPrefix: '/_content/FormCMS/admin/entities',
    authRouterPrefix: '/_content/FormCMS/admin/auth',
    auditLogRouterPrefix: '/_content/FormCMS/admin/audit',
    schemaBuilderRouter: '/schema',
}
console.log({configs})