import 'primereact/resources/themes/lara-light-blue/theme.css'; //theme
import 'primereact/resources/primereact.min.css'; //core css
import 'primeicons/primeicons.css'; //icons
import 'primeflex/primeflex.css'; // flex
import './App.css';
import React from "react";
import {setCmsApiBaseUrl} from "../lib/admin-panel-lib/cms/configs";
import {configs} from "./config";
import {Route, Routes} from "react-router-dom";
import axios from "axios";
import {setAuditLogBaseUrl} from '../lib/admin-panel-lib/auditLog/config';
import {setAuthApiBaseUrl} from '../lib/admin-panel-lib/auth/configs';
import {AuthRouter} from "../lib/admin-panel-lib/auth/AuthRouter";
import {ProgressSpinner} from "primereact/progressspinner";
import {useUserInfo} from "../lib/admin-panel-lib/auth/services/auth";
import {LoginPage} from "./auth/LoginPage";
import {RegisterPage} from "./auth/RegisterPage";
import {SidebarLayout} from "./sidebarLayout/SideBarLayout";
import {GlobalStateKeys, useGlobalState} from "./globalState";
import {TopBarLayout} from "./topbarLayout/TopBarLayout";

axios.defaults.withCredentials = true
setCmsApiBaseUrl(configs.apiURL)
setAuditLogBaseUrl(configs.apiURL)
setAuthApiBaseUrl(configs.apiURL)


function App() {
    const {data: userAccessInfo, error, isLoading} = useUserInfo();
    const [layout, _] = useGlobalState<string>( GlobalStateKeys.Layout, 'sidebar');
    return (
        <>
            {
                userAccessInfo && (layout ==='sidebar' ?<SidebarLayout/>: <TopBarLayout/>)
            }
            {
                error && <Routes>
                    <Route path={`${configs.authRouterPrefix}/*`} element={
                        <AuthRouter
                            baseRouter={configs.authRouterPrefix}
                            LoginPage={LoginPage}
                            RegisterPage={RegisterPage}
                        />
                    }/>
                    <Route path={`*`} element={
                        <AuthRouter
                            baseRouter={configs.authRouterPrefix}
                            LoginPage={LoginPage}
                            RegisterPage={RegisterPage}
                        />
                    }/>
                </Routes>
            }
            {
                isLoading && <ProgressSpinner/>
            }
        </>
    );
}

export default App;