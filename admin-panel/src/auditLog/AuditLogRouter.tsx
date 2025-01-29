import {Route, Routes} from "react-router-dom";
import {AuditLogListWapper} from "./pages/AuditLogList"
import { AuditLogDetail } from "./pages/AuditLogDetail";
export function AuditLogRouter({baseRouter}:{baseRouter:string}) {
    return <Routes>
        <Route path={'/'} element={<AuditLogListWapper baseRouter={baseRouter}/>}/>
        <Route path={'/AuditLog/:id'} element={<AuditLogDetail baseRouter={baseRouter} />}/> 
    </Routes>
}