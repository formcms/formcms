import { FetchingStatus } from "../../components/FetchingStatus"
import { EditDataTable } from "../../components/data/EditDataTable";
import { encodeDataTableState } from "../../components/data/dataTableStateUtil";
import { createColumn } from "../../components/data/columns/createColumn";
import { useDataTableStateManager } from "../../components/data/useDataTableStateManager";

import {useAuditLogs} from "../services/auditLog"
import { XEntity } from "../types/xEntity";


import { useNavigate } from "react-router-dom";
import { useEffect } from "react";
export function AuditLogList({baseRouter,schema}: { baseRouter: string,schema:XEntity }) {
    //entrance
    const initQs = location.search.replace("?","");
    
    //data
    const columns = schema?.attributes?.filter(column => column.inList) ?? [];
    const stateManager = useDataTableStateManager(schema.defaultPageSize, columns,initQs )
    const qs = encodeDataTableState(stateManager.state);
    const {data,error,isLoading}= useAuditLogs(qs)
    var tableColumns = columns.map(x=>createColumn(x,undefined,x.field == schema.labelAttributeName?onEdit:undefined));

    //nav
    useEffect(()=> window.history.replaceState(null,"", `?${qs}`),[stateManager.state]);

    //refrencing
    const navigate = useNavigate();
    
    function onEdit  (rowData:any){
        const url =`${baseRouter}/${schema.name}/${rowData[schema.primaryKey]}?ref=${encodeURIComponent(window.location.href)}`;
        navigate(url);
    }

    return <>
        <FetchingStatus isLoading={isLoading} error={error}/>
        <h2>{schema?.displayName} list</h2>
        <div className="card">
            {data && columns &&<EditDataTable dataKey={schema.primaryKey} columns={tableColumns} data={data} stateManager={stateManager} onView={onEdit}/>}
        </div>
    </>
}