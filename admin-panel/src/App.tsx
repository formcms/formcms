import 'primereact/resources/themes/lara-light-blue/theme.css'; //theme
import 'primereact/resources/primereact.min.css'; //core css
import 'primeicons/primeicons.css'; //icons
import 'primeflex/primeflex.css'; // flex
import './App.css';
import {TopMenuBar} from "./layout/TopMenuBar";
import React  from "react";
import {setCmsApiBaseUrl} from "./cms-client/configs";
import {configs} from "./config";
import {EntityRouter} from "./cms-client/EntityRouter";
import {Route, Routes} from "react-router-dom";
import axios from "axios";
import {useUserInfo} from "./auth/services/auth";
import UserAvatarDropdown from "./layout/UserAvatarDropDown";
import {AccountRouter, NotLoginAccountRouter} from "./auth/AccountRouter";
import { AuditLogRouter } from './auditLog/AuditLogRouter';
import { setAuditLogBaseUrl } from './auditLog/config';
import { setAuthApiBaseUrl } from './auth/configs';
import { DataListPage } from './cms-client/pages/DataListPage';

setCmsApiBaseUrl(configs.apiURL)
setAuditLogBaseUrl(configs.apiURL)
setAuthApiBaseUrl(configs.apiURL)

axios.defaults.withCredentials = true
function App() {

    const {data:profile} = useUserInfo()
    const start = <a href={'/'}><img alt="logo" src={`${configs.adminBaseRouter}/fluent-cms.png`} height="40" className="mr-2"></img></a>;
    const end = (
        <div className="flex align-items-center gap-2">
            <UserAvatarDropdown email={profile?.email??''}/>
        </div>
    );
    return (
        profile? <>
            <TopMenuBar start={start} end={end} profile={profile}/>
            <Routes>
                <Route path={`${configs.entityBaseRouter}/*`} element={<EntityRouter baseRouter={configs.entityBaseRouter}/>}/>
                <Route path={`${configs.authBaseRouter}/*`} element={<AccountRouter baseRouter={configs.authBaseRouter}/>}/>
                <Route path={`${configs.auditLogBaseRouter}/*`} element={<AuditLogRouter baseRouter={configs.auditLogBaseRouter}/>}/>
            </Routes>
        </>:<>
            <Routes>
                <Route path={`${configs.authBaseRouter}/*`} element={<NotLoginAccountRouter/>}/>
                <Route path={configs.adminBaseRouter} element={<NotLoginAccountRouter />} />
            </Routes>
            </>
    );
}
export default App;