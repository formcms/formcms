import React, {useRef} from 'react';
import {Avatar} from 'primereact/avatar';
import {Menu} from 'primereact/menu';
import CryptoJS from 'crypto-js';
import {MenuItem} from "primereact/menuitem";
import {useUserInfo} from "../../lib/admin-panel-lib/auth/services/auth";
import {useUserProfileMenu} from "../../lib/admin-panel-lib/useMenuItems";
import {configs} from "../config";

const getAvatarUrl = (email: string) => {
    const trimmedEmail = email.trim().toLowerCase();
    const hash = CryptoJS.MD5(trimmedEmail).toString();
    return `https://www.gravatar.com/avatar/${hash}`;
};

const UserAvatarDropdown = () => {
    const menu = useRef<any>(null);
    const {data:userAccessInfo} = useUserInfo();
    const userProfileMenu: MenuItem[] = useUserProfileMenu(configs.authRouterPrefix);
    const imageUrl = getAvatarUrl(userAccessInfo?.email??'');

    return (
        <div className="flex align-items-center gap-2">
            <Avatar image={imageUrl} shape="circle" onClick={(event) => menu?.current?.toggle(event)}
                    style={{cursor: 'pointer'}}/>
            <Menu model={userProfileMenu} popup ref={menu}/>
            <span onClick={(event) => menu?.current?.toggle(event)}
                  style={{cursor: 'pointer'}}>{userAccessInfo?.email.split('@')[0]}</span>
        </div>
    );
};

export default UserAvatarDropdown;
