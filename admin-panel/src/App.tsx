import 'primereact/resources/themes/lara-light-blue/theme.css'; //theme
import 'primereact/resources/primereact.min.css'; //core css
import 'primeicons/primeicons.css'; //icons
import 'primeflex/primeflex.css'; // flex
import './App.css';
import React from "react";
import {setCmsApiBaseUrl} from "../lib/admin-panel-lib/cms/configs";
import {configs} from "./config";
import {EntityRouter} from "../lib/admin-panel-lib/cms/EntityRouter";
import {Route, Routes} from "react-router-dom";
import axios from "axios";
import {AccountRouter} from "../lib/admin-panel-lib/auth/AccountRouter";
import {AuditLogRouter} from '../lib/admin-panel-lib/auditLog/AuditLogRouter';
import {setAuditLogBaseUrl} from '../lib/admin-panel-lib/auditLog/config';
import {setAuthApiBaseUrl} from '../lib/admin-panel-lib/auth/configs';
import {TopMenuBar} from "./layout/TopMenuBar";
import {AuthRouter} from "../lib/admin-panel-lib/auth/AuthRouter";
import {ProgressSpinner} from "primereact/progressspinner";
import {useUserInfo} from "../lib/admin-panel-lib/auth/services/auth";

axios.defaults.withCredentials = true
setCmsApiBaseUrl(configs.apiURL)
setAuditLogBaseUrl(configs.apiURL)
setAuthApiBaseUrl(configs.apiURL)


function App() {
    const {data: userAccessInfo, error, isLoading} = useUserInfo();
    return (
        <>
            {
                userAccessInfo && (
                    <>
                        <TopMenuBar/>
                        <Routes>
                            <Route path={`${configs.entityRouterPrefix}/*`}
                                   element={<EntityRouter baseRouter={configs.entityRouterPrefix}/>}/>
                            <Route path={`${configs.authRouterPrefix}/*`}
                                   element={<AccountRouter baseRouter={configs.authRouterPrefix}/>}/>
                            <Route path={`${configs.auditLogRouterPrefix}/*`}
                                   element={<AuditLogRouter baseRouter={configs.auditLogRouterPrefix}/>}/>
                        </Routes>
                    </>
                )
            }
            {
                error && <Routes>
                    <Route path={`${configs.authRouterPrefix}/*`} element={<AuthRouter baseRouter={configs.authRouterPrefix}/>}/>
                    <Route path={`*`} element={<AuthRouter baseRouter={configs.authRouterPrefix} />}/>
                </Routes>
            }
            {
                isLoading && <ProgressSpinner/>
            }
        </>
    );
}

export default App;