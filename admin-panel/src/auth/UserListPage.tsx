import {useUserListPage} from "../../lib/admin-panel-lib/auth/pages/useUserListPage";

export function UserListPage() {
    const {UserListPageMain} = useUserListPage()
    return <>
        <h2>User list</h2>
        <div className="card"></div>
        <UserListPageMain/>
    </>
}