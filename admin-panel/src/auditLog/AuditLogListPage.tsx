import {useAuditLogListPage} from "../../lib/admin-panel-lib/auditLog/pages/useAuditLogListPage";
import {EntityPageProps} from "../../lib/admin-panel-lib/cms/EntityRouter";

export function AuditLogListPage({baseRouter, schema}: EntityPageProps) {
    const {AuditLogListPageMain} = useAuditLogListPage(baseRouter, schema);
    return <>
        <h2>{schema?.displayName} list</h2>
        <AuditLogListPageMain/>
    </>
}