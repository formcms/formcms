import useSWR from "swr";
import {fullCmsApiUrl } from "../configs";
import {decodeError, fetcher, swrConfig} from "../../services/util";
import { XEntity } from "../types/schemaExt";

export function useSchema (schemaName:string){
    let { data,error,isLoading} = useSWR<XEntity>(fullCmsApiUrl(`/schemas/entity/${schemaName}`), fetcher, swrConfig)
    if (error){
        error = decodeError(error)
    }
    return {data, isLoading, error}
}