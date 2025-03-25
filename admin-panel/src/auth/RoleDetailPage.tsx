import {Button} from "primereact/button";
import {useRoleDetailPage} from "../../lib/admin-panel-lib/auth/pages/useRoleDetailPage";
import {BaseRouterProps} from "../../lib/admin-panel-lib/auth/AccountRouter";

export function RoleDetailPage({baseRouter}:BaseRouterProps) {
    const {isNewRole, roleData, handleDelete, RoleDetailPageMain} = useRoleDetailPage(baseRouter)
    return <>
        { isNewRole&& <h2>Editing Role `{roleData?.name}`</h2>}
        <Button type={'submit'} label={"Save Role"} icon="pi pi-check"/>
        {' '}
        <Button type={'button'} label={"Delete Role"} severity="danger" onClick={handleDelete}/>
        <RoleDetailPageMain/>
    </>
}