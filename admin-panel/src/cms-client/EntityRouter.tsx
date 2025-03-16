import {Route, Routes,} from "react-router-dom";
import {DataListPage, NewItemRoute} from "./pages/DataListPage";
import {DataItemPage} from "./pages/DataItemPage";
import {NewDataItemPage} from "./pages/NewDataItemPage";
import { XEntityWrapper } from "../components/XEntityWrapper";
import { useTaskEntity } from "./services/task";
import { TaskList } from "./pages/TaskList";
import { AssetList } from "./pages/AssetList";
import { useAssetEntityWithLink } from "./services/asset";
import { AssetEdit } from "./pages/AssetEdit";

export const TasksRouter = '/tasks'
export const AssetsRouter = '/assets'
export function EntityRouter({baseRouter}:{baseRouter:string}) {
    return <Routes>
        <Route path={'/:schemaName'} element={<DataListPage baseRouter={baseRouter}/>}> </Route>
        <Route path={`/:schemaName/${NewItemRoute}`} element={<NewDataItemPage baseRouter={baseRouter}/>}> </Route>
        <Route path={'/:schemaName/:id'} element={<DataItemPage baseRouter={baseRouter}/>}> </Route>
        <Route path={TasksRouter} 
               element={<XEntityWrapper baseRouter={baseRouter} Component={TaskList} useEntityHook={useTaskEntity}/>}
        > </Route>
        <Route path={AssetsRouter} element={
            <XEntityWrapper 
                baseRouter={baseRouter + AssetsRouter} 
                Component={AssetList}
                useEntityHook={useAssetEntityWithLink}/>
        }> </Route>
        <Route path={`${AssetsRouter}/:id`} element={
            <XEntityWrapper
                baseRouter={baseRouter}
                Component={AssetEdit}
                useEntityHook={useAssetEntityWithLink}
            />
        }/>
    </Routes>
}