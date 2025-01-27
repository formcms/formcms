import useSWR from "swr";
import {fullCmsApiUrl} from "../configs";
import axios from "axios";
import {catchResponse, decodeError, fetcher, swrConfig} from "../../services/util";
import { LookupListResponse } from "../types/lookupListResponse";
import { ListResponse } from "../types/listResponse";

export function useListData(schemaName: string | undefined, qs: string) {
    let res = useSWR<ListResponse>(fullCmsApiUrl(`/entities/${schemaName}?${qs}`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export function useTreeData(schemaName: string | undefined ) {
    let res = useSWR<any[]>(fullCmsApiUrl(`/entities/tree/${schemaName}`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}
export function useItemData(schemaName: string, id: any) {
    let res =  useSWR(fullCmsApiUrl(`/entities/${schemaName}/${id}`), fetcher, swrConfig)
    return {...res, error:decodeError(res.error)}
}

export async function updateItem(schemaName:string,item:any){
    return catchResponse(()=>axios.post(fullCmsApiUrl(`/entities/${schemaName}/update`),item))
}

export async function addItem(schemaName:string, item:any){
    return catchResponse(()=>axios.post(fullCmsApiUrl(`/entities/${schemaName}/insert`),item))
}

export async function deleteItem(schemaName:string, item:any){
    return catchResponse(()=>axios.post(fullCmsApiUrl(`/entities/${schemaName}/delete`), item))
}

export async function savePublicationSettings(schemaName:string, item:any){
    return catchResponse(()=>axios.post(fullCmsApiUrl(`/entities/${schemaName}/publication`), item))
}

export function useJunctionIds(schemaName: string, id: any, field:string) {
    const url= fullCmsApiUrl(`/entities/junction/target_ids/${schemaName}/${id}/${field}`);
    let res = useSWR<any[]>(schemaName &&id &&field ?  url:null, fetcher,swrConfig)
    return {...res, error:decodeError(res.error)}
}
export function useJunctionData(schemaName: string, id:any, field:string, exclude:boolean, qs:string ) {
    const url= fullCmsApiUrl(`/entities/junction/${schemaName}/${id}/${field}?exclude=${exclude}&${qs}`);
    let res = useSWR<ListResponse>(schemaName &&id &&field ?  url:null, fetcher,swrConfig)
    return {...res, error:decodeError(res.error)}
}

export async function saveJunctionItems(schemaName: string, id:any, field:string, items:any ) {
    return catchResponse(()=>axios.post( fullCmsApiUrl(`/entities/junction/${schemaName}/${id}/${field}/save`), items))
}

export async function deleteJunctionItems(schemaName:string, id:any, field:string, items: any){
    return catchResponse(()=>axios.post(fullCmsApiUrl(`/entities/junction/${schemaName}/${id}/${field}/delete`), items))
}

export function useCollectionData(schemaName: string, id:any, field:string, qs:string ) {
    const url =  fullCmsApiUrl(`/entities/collection/${schemaName}/${id}/${field}?${qs}`);
    let res = useSWR(schemaName &&id &&field  ?url:null, fetcher,swrConfig)
    return {...res, error:decodeError(res.error)}   
}

export async function addCollectionItem(schemaName: string, id:any, field:string, item:any ) {
    return catchResponse(()=>axios.post(fullCmsApiUrl(`/entities/collection/${schemaName}/${id}/${field}/insert`),item))
}

export function getLookupData(schemaName: string, query: string){
    return catchResponse(()=>axios.get<LookupListResponse>(fullCmsApiUrl(`/entities/lookup/${schemaName}?query=${encodeURIComponent(query)}`)))
}

export function useLookupData(schemaName: string, query: string) {
    let res = useSWR<LookupListResponse>(fullCmsApiUrl(`/entities/lookup/${schemaName}?query=${encodeURIComponent(query)}`), fetcher, swrConfig);
    return {...res, error: decodeError(res.error)}
}

