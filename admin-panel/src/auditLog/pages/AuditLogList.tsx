import { FetchingStatus } from "../../components/FetchingStatus"
import { LazyDataTable } from "../../components/dataTable/LazyDataTable";
import {useAuditLogs, useAuditLogsEntity} from "../services/auditLog"
import { XEntity } from "../types/xEntity";
import { useLazyStateHandlers } from "../../components/dataTable/useLazyStateHandlers";
import { encodeLazyState } from "../../components/dataTable/lazyStateUtil";
export function AuditLogListWapper({baseRouter}: { baseRouter: string }) {
    const {data:schema,error,isLoading} = useAuditLogsEntity()
    return <>
        <FetchingStatus isLoading={isLoading} error={error}/>
        {schema&&<AuditLogList {...{baseRouter,schema}}/>}
    </>
}

export function AuditLogList({baseRouter,schema}: { baseRouter: string,schema:XEntity }) {
    const columns = schema?.attributes?.filter(column => column.inList) ?? [];
    let {lazyState, eventHandlers} = useLazyStateHandlers(schema.defaultPageSize, columns,undefined )
    const {data,error,isLoading}= useAuditLogs(encodeLazyState(lazyState))

    return <>
        <FetchingStatus isLoading={isLoading} error={error}/>
        <h2>{schema?.displayName} list</h2>
        <div className="card">
            {schema && data && columns &&<LazyDataTable {...{baseRouter, schema, columns, data, lazyState, eventHandlers}}/>}
        </div>
    </>
}

