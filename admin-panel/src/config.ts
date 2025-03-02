
export const configs = {
    apiURL: import.meta.env.VITE_REACT_APP_API_URL,
    entityBaseRouter: '/_content/FormCMS/admin/entities',
    authBaseRouter: '/_content/FormCMS/admin/auth',
    adminBaseRouter: '/_content/FormCMS/admin',
    auditLogBaseRouter: '/_content/FormCMS/admin/audit',
}
console.log({configs})