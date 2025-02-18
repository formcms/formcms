import {Route, Routes,} from "react-router-dom";
import {DataListPage, NewItemRoute} from "./pages/DataListPage";
import {DataItemPage} from "./pages/DataItemPage";
import {NewDataItemPage} from "./pages/NewDataItemPage";
import { XEntityWrapper } from "../components/XEntityWrapper";
import { useTaskEntity } from "./services/task";
import { TaskList } from "./pages/TaskList";

export const TasksRouter = '/tasks'
export function EntityRouter({baseRouter}:{baseRouter:string}) {
    return <Routes>
            <Route path={'/:schemaName/'} element={<DataListPage baseRouter={baseRouter}/>}> </Route>
            <Route path={`/:schemaName/${NewItemRoute}`} element={<NewDataItemPage baseRouter={baseRouter}/>}> </Route>
            <Route path={'/:schemaName/:id'} element={<DataItemPage baseRouter={baseRouter}/>}> </Route>
            <Route path={TasksRouter} element={<XEntityWrapper baseRouter={baseRouter} Component={TaskList} useEntityHook={useTaskEntity}/>}> </Route>
    </Routes>
}