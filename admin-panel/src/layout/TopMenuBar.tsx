import {Menubar} from 'primereact/menubar';
import React from "react";
import {useTopMenuBar} from "../auth/services/menu";
import { useNavigate} from "react-router-dom";
import {configs} from "../config";
import {RoleRoute, UserRoute} from "../auth/AccountRouter";
import { UserAccess } from '../auth/types/userAccess';
import {AssetsRouter, TasksRouter } from '../cms-client/EntityRouter';


const entityPrefix = '/entities'
export const  MenuSchemaBuilder = "menu_schema_builder";
export const  MenuUsers = "menu_users";
export const  MenuRoles = "menu_roles";
export const  MenuAuditLog = "menu_audit_log";
export const  MenuTasks = "menu_tasks";

export const MenuAssets = "menu_assets";

export function TopMenuBar({start, end, profile}:{start:any, end:any, profile: UserAccess}) {
    const navigate = useNavigate();
    const items = useTopMenuBar().filter(x=>{
        if (profile.roles.includes('sa')){
            return true;
        }

        if (!x.url.startsWith(entityPrefix)){
            return true;
        }

        const entityName = x.url.substring(entityPrefix.length + 1);
        return profile?.readWriteEntities?.includes(entityName)
            || profile?.restrictedReadWriteEntities?.includes(entityName)
            || profile?.readonlyEntities?.includes(entityName)
            || profile?.restrictedReadonlyEntities?.includes(entityName);
    })
    const links = items.map((x: any)=> {
            if (x.isHref) {
                return x;
            }
            const url = x.url.replaceAll(entityPrefix, configs.entityBaseRouter);
            return {
                url,
                icon: 'pi ' + (x.icon === '' ? 'pi-bolt' : x.icon),
                label: x.label,
                command: () => {
                    navigate(url)
                }
            };
        }
    );

    [
        {
            key: MenuTasks,
            icon: 'pi pi-cog',
            label: 'Tasks',
            command: () => {
                navigate(`${configs.entityBaseRouter}${TasksRouter}`)
            }
        },
        {
            key: MenuAssets,
            icon: 'pi pi-cog',
            label: 'Assets',
            command: () => {
                navigate(`${configs.entityBaseRouter}${AssetsRouter}`)
            }
        },
        
        {
            key: MenuRoles,
            icon: 'pi pi-sitemap',
            label: 'Roles',
            command: () => {
                navigate(`${configs.authBaseRouter}${RoleRoute}`)
            }
        },
        {
            key: MenuUsers,
            icon: 'pi pi-users',
            label: 'Users',
            command: () => {
                navigate(`${configs.authBaseRouter}${UserRoute}`)
            }
        },
        {
            key: MenuAuditLog,
            icon: 'pi pi-file-edit',
            label: 'Audit Log',
            command: () => {
                navigate(`${configs.auditLogBaseRouter}`)
            }
        },
       
        {
            key: MenuSchemaBuilder,
            icon: 'pi pi-cog',
            label: 'Schema Builder',
            url: '/schema'
        },
    ].forEach(x=>{
        if (profile?.allowedMenus?.includes(x.key)){
            links.push(x)
        }
    });

   
    return (
        <Menubar model={links} start={start} end={end}/>
    )
}