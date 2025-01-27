let apiBaseURL = "";
export function setAuditLogBaseUrl(v: string) {
    apiBaseURL = v;
}
export function fullAuditLogUrl (subPath :string){
    return apiBaseURL + subPath
}