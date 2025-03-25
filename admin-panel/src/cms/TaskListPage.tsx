import {Button} from "primereact/button";
import {useTaskListPage} from "../../lib/admin-panel-lib/cms/pages/useTaskListPage";
import {EntityPageProps} from "../../lib/admin-panel-lib/cms/EntityRouter";

export function TaskListPage({schema}:EntityPageProps){
    const {handleAddExportTask, handleAddImportTask, handleImportDemoData, TaskListMain, CheckErrorStatus} = useTaskListPage({schema});
    return <>
        <h2>{schema?.displayName} list</h2>
        <Button onClick={handleAddExportTask}>Add Export Task</Button>{' '}
        <Button onClick={handleAddImportTask}>Add Import Task</Button>{' '}
        <Button onClick={handleImportDemoData}>Import Demo Data</Button>
        <CheckErrorStatus/>
        <TaskListMain/>
    </>
}