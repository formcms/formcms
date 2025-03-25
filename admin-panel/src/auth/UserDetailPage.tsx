import {Button} from "primereact/button";
import {useUserDetailPage} from "../../lib/admin-panel-lib/auth/pages/useUserDetailPage";
import {BaseRouterProps} from "../../lib/admin-panel-lib/auth/AccountRouter";

export function UserDetailPage({baseRouter}:BaseRouterProps) {
    const {formId, userData,handleDelete, UserDetailPageMain} = useUserDetailPage(baseRouter)
    return   <>
        <h2>Editing {userData?.email}</h2>
        <Button type={'submit'} label={"Save User"} icon="pi pi-check" form={formId}/>
        {' '}
        <Button type={'button'} label={"Delete User"} severity="danger" onClick={handleDelete}/>
        <UserDetailPageMain/>
    </>
}