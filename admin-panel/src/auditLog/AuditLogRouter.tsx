import {Route, Routes} from "react-router-dom";
import {AuditLogListWapper} from "./pages/AuditLogList"
export function AuditLogRouter({baseRouter}:{baseRouter:string}) {
    return <Routes>
        <Route path={'/'} element={<AuditLogListWapper baseRouter={baseRouter}/>}/>
    </Routes>
}