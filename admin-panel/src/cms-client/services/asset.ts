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
export  function useAssetEntityWithLink() {
    let res = useSWR<XEntity>(fullCmsApiUrl(`/assets/entity?linkCount=${true}`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export  function useAssetEntity() {
    let res = useSWR<XEntity>(fullCmsApiUrl(`/assets/entity?linkCount=${false}`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}
export  function useAssets(qs:string,withLinkCount:boolean) {
    let res = useSWR<ListResponse>(fullCmsApiUrl(`/assets?linkCount=${withLinkCount}&${qs}`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export function getAssetReplaceUrl(id:number){
    return  fullCmsApiUrl(`/assets/${id}`);
}

export function getFileUploadURL (){
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

export function updateAssetMeta (asset:any) {
    return catchResponse(()=>axios.post(fullCmsApiUrl(`/assets/meta/`),asset))
}