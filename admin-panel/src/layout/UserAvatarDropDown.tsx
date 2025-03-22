import React, {useRef} from 'react';
import {Avatar} from 'primereact/avatar';
import {Menu} from 'primereact/menu';
import CryptoJS from 'crypto-js';
import {logout, useUserInfo} from "../../lib/admin-panel-lib/auth/services/auth";
import { useNavigate} from "react-router-dom";
import {configs} from "../config";
import {ChangePasswordRoute} from "../../lib/admin-panel-lib/auth/AccountRouter";

const getGravatarUrl = (email: string) => {
    const trimmedEmail = email.trim().toLowerCase();
    const hash = CryptoJS.MD5(trimmedEmail).toString();
    return `https://www.gravatar.com/avatar/${hash}`;
};

const UserAvatarDropdown = ({email}: { email: string }) => {
    const navigate = useNavigate();
    const {mutate} = useUserInfo();

    const menu = useRef<any>(null);
    const items = [
        {
            label: 'Change Password',
            icon: 'pi pi-lock',
            command: ()=>navigate(`${configs.authBaseRouter}${ChangePasswordRoute}`)
        },
        {
            label: 'Logout',
            icon: 'pi pi-sign-out',
            command: async () => {
                await logout();
                await mutate();
                window.location.href = '/';
            }
        }
    ];

    const gravatarUrl = getGravatarUrl(email);

    return (
        <div className="flex align-items-center gap-2">
            <Avatar image={gravatarUrl} shape="circle" onClick={(event) => menu?.current?.toggle(event)}
                    style={{cursor: 'pointer'}}/>
            <Menu model={items} popup ref={menu}/>
            <span onClick={(event) => menu?.current?.toggle(event)}
                  style={{cursor: 'pointer'}}>{email.split('@')[0]}</span>
        </div>
    );
};

export default UserAvatarDropdown;
