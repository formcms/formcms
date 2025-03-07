import useSWR from "swr";
import { XEntity } from "../types/xEntity";
import { ListResponse } from "../types/listResponse";
import { fullCmsApiUrl } from "../configs";
import {catchResponse, decodeError, fetcher, swrConfig } from "../../services/util";
import axios from "axios";
import { Asset } from "../types/asset";

export function useSingleAsset(id: any){
    let res = useSWR<Asset>(fullCmsApiUrl(`/assets/${id}`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}
export  function useAssetWithLinkCountEntity() {
    let res = useSWR<XEntity>(fullCmsApiUrl(`/assets/entity?count=${true}`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export  function useAssetEntity() {
    let res = useSWR<XEntity>(fullCmsApiUrl(`/assets/entity?count=${false}`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}
export  function useAssets(qs:string,countLink:boolean) {
    let res = useSWR<ListResponse>(fullCmsApiUrl(`/assets?count=${countLink}&${qs}`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export function getAssetReplaceUrl(path:string){
    return  fullCmsApiUrl(`/assets?path=${path}`);
}

export function getFileUploadURL (path:string){
    return  fullCmsApiUrl('/assets');
}

export function useGetCmsAssetsUrl (){
    const { data: assetBaseUrl } = useSWR<string>(fullCmsApiUrl(`/assets/base`), fetcher, swrConfig);
    return (url: string) => {
        if (!url) return url;
        return url.startsWith('http') ? url : `${assetBaseUrl || ''}${url}`;
    }
}

export function deleteAsset (id:number) {
    return catchResponse(()=>axios.post(fullCmsApiUrl(`/assets/delete/${id}/`)))
}