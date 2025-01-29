import { useParams } from "react-router-dom"
import { useAuditLogsDetail } from "../services/auditLog"
import { Button } from "primereact/button"


export function AuditLogDetail({baseRouter}:{baseRouter:string}) {
    const {id} = useParams()
    const {data} = useAuditLogsDetail(id!)    
    if (!data) {
        return <></>
    }
    const referingUrl = new URLSearchParams(location.search).get("ref") ?? baseRouter;
    
    return <>
        <Button type={'button'} label={"Return"}  onClick={()=>window.location.href = referingUrl}/>
        <div className="surface-section">
            <div className="font-medium text-3xl text-900 mb-3">[{data.action} {data.entityName}] {data.recordId} - {data.recordLabel}</div>
            <div className="text-500 mb-5"> By User: <b>{data.userName}({data.userId})</b> At <b>{data.createdAt.toString()}</b></div>
            
            <ul className="list-none p-0 m-0">
                {Object.entries(data.payload).map(([k,v]) => (
                    <li className="flex align-items-center py-3 px-2 border-top-1 surface-border flex-wrap">
                        <div className="text-500 w-6 md:w-2 font-medium">{k}</div>
                        <div className="text-900 w-full md:w-8 md:flex-order-0 flex-order-1">{v}</div>
                    </li>
                ))}
            </ul>
        </div>
    </>
}