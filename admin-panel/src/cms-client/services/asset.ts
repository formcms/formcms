import useSWR from "swr";
import { XEntity } from "../types/xEntity";
import { ListResponse } from "../types/listResponse";
import { fullCmsApiUrl } from "../configs";
import {decodeError, fetcher, swrConfig } from "../../services/util";

export  function useAssetEntity() {
    let res = useSWR<XEntity>(fullCmsApiUrl(`/assets/entity`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}
export  function useAssets(qs:string) {
    let res = useSWR<ListResponse>(fullCmsApiUrl(`/assets?${qs}`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export function getFileUploadURL (){
    return  fullCmsApiUrl('/assets');
}

export function useGetCmsAssetsUrl (){
    const { data: assetBaseUrl } = useSWR<string>(fullCmsApiUrl(`/assets/base`), fetcher, swrConfig);
    return (url: string) => {
        if (!url) return url;
        
        return url.startsWith('http') ? url : `${assetBaseUrl || ''}/${url}`;
    }
}