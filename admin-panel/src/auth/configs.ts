let apiBaseURL = "";
export function setAuthApiBaseUrl(v: string) {
    apiBaseURL = v;
}
export function fullAuthApiUrl (subPath :string){
    return apiBaseURL + subPath
}