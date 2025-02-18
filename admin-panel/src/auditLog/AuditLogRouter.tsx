import {Route, Routes} from "react-router-dom";
import {AuditLogList} from "./pages/AuditLogList"
import { AuditLogDetail } from "./pages/AuditLogDetail";
import { XEntityWrapper } from "../components/XEntityWrapper";
import { useAuditLogsEntity } from "./services/auditLog";
export function AuditLogRouter({baseRouter}:{baseRouter:string}) {
    return <Routes>
        <Route path={'/'} element={<XEntityWrapper Component={AuditLogList} useEntityHook={useAuditLogsEntity} baseRouter={baseRouter}/>}/>
        <Route path={'/AuditLog/:id'} element={<AuditLogDetail baseRouter={baseRouter} />}/> 
    </Routes>
}