import {Route, Routes} from "react-router-dom";
import {configs} from "./config";
import {EntityRouter} from "../lib/admin-panel-lib/cms/EntityRouter";
import {DataListPage} from "./cms/DataListPage";
import {NewDataItemPage} from "./cms/NewDataItemPage";
import {DataItemPage} from "./cms/DataItemPage";
import {TaskListPage} from "./cms/TaskListPage";
import {AssetListPage} from "./cms/AssetListPage";
import {AssetEditPage} from "./cms/AssetEditPage";
import {AccountRouter} from "../lib/admin-panel-lib/auth/AccountRouter";
import {UserListPage} from "./auth/UserListPage";
import {UserDetailPage} from "./auth/UserDetailPage";
import {ChangePasswordPage} from "./auth/ChangePasswordPage";
import {RoleListPage} from "./auth/RoleListPage";
import {RoleDetailPage} from "./auth/RoleDetailPage";
import {AuditLogRouter} from "../lib/admin-panel-lib/auditLog/AuditLogRouter";
import {AuditLogDetailPage} from "./auditLog/AuditLogDetailPage";
import {AuditLogListPage} from "./auditLog/AuditLogListPage";
import React from "react";

export function AppRouters() {
    return <Routes>
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
}