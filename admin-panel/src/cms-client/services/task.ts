import useSWR from "swr";
import { XEntity } from "../types/xEntity";
import { fullCmsApiUrl } from "../configs";
import {catchResponse, decodeError, fetcher, swrConfig } from "../../services/util";
import { ListResponse } from "../types/listResponse";
import axios from "axios";

export  function useTaskEntity() {
    let res = useSWR<XEntity>(fullCmsApiUrl(`/tasks/entity`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}
export  function useTasks(qs:string) {
    let res = useSWR<ListResponse>(fullCmsApiUrl(`/tasks?${qs}`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export  function addExportTask() {
    return catchResponse(()=>axios.post(fullCmsApiUrl(`/tasks/export`)));
}

export  function archiveExportTask(id:number) {
    return catchResponse(()=>axios.post(fullCmsApiUrl(`/tasks/export/archive/${id}`),{}));
}

export  function getExportTaskDownloadFileLink(id:number) {
    return fullCmsApiUrl(`/tasks/export/download/${id}`);
}