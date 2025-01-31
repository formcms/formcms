import { FetchingStatus } from "../../components/FetchingStatus"
import { EditDataTable } from "../../components/dataTable/EditDataTable";
import {useAuditLogs, useAuditLogsEntity} from "../services/auditLog"
import { XEntity } from "../types/xEntity";
import { useDataTableStateManager } from "../../components/dataTable/useDataTableStateManager";
import { encodeDataTableState } from "../../components/dataTable/dataTableStateUtil";
import { createColumn } from "../../components/dataTable/columns/createColumn";
import { useNavigate } from "react-router-dom";
export function AuditLogListWapper({baseRouter}: { baseRouter: string }) {
    const {data:schema,error,isLoading} = useAuditLogsEntity()
    return <>
        <FetchingStatus isLoading={isLoading} error={error}/>
        {schema&&<AuditLogList {...{baseRouter,schema}}/>}
    </>
}

export function AuditLogList({baseRouter,schema}: { baseRouter: string,schema:XEntity }) {
    const navigate = useNavigate();    
    const columns = schema?.attributes?.filter(column => column.inList) ?? [];
    const stateManager = useDataTableStateManager(schema.defaultPageSize, columns,undefined )
    const {data,error,isLoading}= useAuditLogs(encodeDataTableState(stateManager.state))
    const onEdit = (rowData:any)=>{
        const url =`${baseRouter}/${schema.name}/${rowData[schema.primaryKey]}?ref=${encodeURIComponent(window.location.href)}`;
        navigate(url);
    }
    
    var tableColumns = columns.map(x=>createColumn(x,undefined,x.field == schema.labelAttributeName?onEdit:undefined));

    return <>
        <FetchingStatus isLoading={isLoading} error={error}/>
        <h2>{schema?.displayName} list</h2>
        <div className="card">
            {data && columns &&<EditDataTable columns={tableColumns} data={data} stateManager={stateManager} onView={onEdit}/>}
        </div>
    </>
}