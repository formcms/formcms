import useSWR from "swr";
import { fullAuditLogUrl } from "../config";
import {decodeError, fetcher, swrConfig} from "../../services/util";
import { XEntity } from "../types/xEntity";
import { ListResponse } from "../types/listResponse";


export  function useAuditLogsEntity() {
    let res = useSWR<XEntity>(fullAuditLogUrl(`/audit_log/entity`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}
export  function useAuditLogs() {
    let res = useSWR<ListResponse>(fullAuditLogUrl(`/audit_log`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}