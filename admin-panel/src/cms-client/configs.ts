let apiBaseURL = "";
let assetsBaseURL = "";
export function setCmsApiBaseUrl(v: string) {
    apiBaseURL = v;
}
export function fullCmsApiUrl (subPath :string){
    return apiBaseURL + subPath
}

export function fileUploadURL (){
    return apiBaseURL + '/files'
}

export function setCmsAssetBaseUrl (baseURL:string){
    assetsBaseURL = baseURL;
}
export function getFullCmsAssetUrl(url: string) {
    return assetsBaseURL + url;
}