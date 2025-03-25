import React, {useRef} from 'react';
import {Avatar} from 'primereact/avatar';
import {Menu} from 'primereact/menu';
import {MenuItem} from "primereact/menuitem";
import {useUserInfo} from "../../lib/admin-panel-lib/auth/services/auth";
import {useUserProfileMenu} from "../../lib/admin-panel-lib/useMenuItems";
import {configs} from "../config";

const UserAvatarDropdown = () => {
    const menu = useRef<any>(null);
    const {data:userAccessInfo} = useUserInfo();
    const userProfileMenu: MenuItem[] = useUserProfileMenu(configs.authRouterPrefix);
    function handleToggle(event:any){
        menu?.current?.toggle(event);
    }

    return (
        <div className="flex align-items-center gap-2">
            <Avatar onClick={handleToggle} icon="pi pi-user" size="normal" style={{ backgroundColor: '#2196F3', color: '#ffffff' }} shape="circle" />
            <Menu model={userProfileMenu} popup ref={menu}/>
            <span onClick={handleToggle} style={{cursor: 'pointer'}}>{userAccessInfo?.email.split('@')[0]}</span>
        </div>
    );
};

export default UserAvatarDropdown;
