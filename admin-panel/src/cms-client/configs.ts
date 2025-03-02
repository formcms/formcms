let apiBaseURL = "";
export function setCmsApiBaseUrl(v: string) {
    apiBaseURL = v;
}
export function fullCmsApiUrl (subPath :string){
    return apiBaseURL + subPath
}