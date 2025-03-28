import {useUserListPage} from "../../lib/admin-panel-lib/auth/pages/useUserListPage";
import {GlobalStateKeys, useGlobalState} from "../globalState";

export function UserListPage() {
    const [layout, _] = useGlobalState<string>( GlobalStateKeys.Layout, 'sidebar');
    const {UserListPageMain} = useUserListPage()
    return <>
        {layout !=='sidebar' && <h2>User list</h2>}
        <UserListPageMain/>
    </>
}