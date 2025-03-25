import {Button} from "primereact/button";
import {BaseRouterProps} from "../../lib/admin-panel-lib/auth/AccountRouter";
import {useRoleListPage} from "../../lib/admin-panel-lib/auth/pages/useRoleListPage";

export  function RoleListPage({baseRouter}:BaseRouterProps) {
    const {handleNavigateToNewRolePage,RoleListPageMain} = useRoleListPage(baseRouter);
    return <>
        <h2>Role list</h2>
        <Button onClick={handleNavigateToNewRolePage}>Create New Role</Button>
        <RoleListPageMain/>
    </>
}