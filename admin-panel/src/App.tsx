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
import {DataListPage} from "./cms/DataListPage";
import {NewDataItemPage} from "./cms/NewDataItemPage";
import {DataItemPage} from "./cms/DataItemPage";
import {TaskListPage} from "./cms/TaskListPage";
import {AssetListPage} from "./cms/AssetListPage";
import {AssetEditPage} from "./cms/AssetEditPage";
import {ChangePasswordPage} from "./auth/ChangePasswordPage";
import {UserListPage} from "./auth/UserListPage";
import {LoginPage} from "./auth/LoginPage";
import {RegisterPage} from "./auth/RegisterPage";
import {RoleListPage} from "./auth/RoleListPage";
import {RoleDetailPage} from "./auth/RoleDetailPage";
import {AuditLogDetailPage} from "./auditLog/AuditLogDetailPage";
import {AuditLogListPage} from "./auditLog/AuditLogListPage";
import {UserDetailPage} from "./auth/UserDetailPage";

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
                            <Route path={`${configs.entityRouterPrefix}/*`} element={
                                <EntityRouter
                                    baseRouter={configs.entityRouterPrefix}
                                    DataListPage={DataListPage}
                                    NewDataItemPage={NewDataItemPage}
                                    DataItemPage={DataItemPage}
                                    TaskListPage={TaskListPage}
                                    AssetListPage={AssetListPage}
                                    AssetEditPage={AssetEditPage}
                                />
                            }/>
                            <Route path={`${configs.authRouterPrefix}/*`} element={
                                <AccountRouter
                                    baseRouter={configs.authRouterPrefix}
                                    UserListPage={UserListPage}
                                    UserDetailPage={UserDetailPage}
                                    ChangePasswordPage={ChangePasswordPage}
                                    RoleListPage={RoleListPage}
                                    RoleDetailPage={RoleDetailPage}
                                />
                            }/>
                            <Route path={`${configs.auditLogRouterPrefix}/*`} element={
                                <AuditLogRouter
                                    baseRouter={configs.auditLogRouterPrefix}
                                    AuditLogDetailPage={AuditLogDetailPage}
                                    AuditLogListPage={AuditLogListPage}
                                />
                            }/>
                        </Routes>
                    </>
                )
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