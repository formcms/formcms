import { Button } from "primereact/button";
import { createColumn } from "../../components/dataTable/columns/createColumn";
import { encodeDataTableState } from "../../components/dataTable/dataTableStateUtil";
import { EditDataTable } from "../../components/dataTable/EditDataTable";
import { useDataTableStateManager } from "../../components/dataTable/useDataTableStateManager";
import { FetchingStatus } from "../../components/FetchingStatus";
import {addExportTask, archiveExportTask, getAddImportTaskUploadUrl, getExportTaskDownloadFileLink,
    importDemoData, useTasks } from "../services/task";
import { XEntity } from "../types/xEntity";
import { useCheckError } from "../../components/useCheckError";
import { Column } from "primereact/column";
import { SystemTask } from "../types/systemTask";
import { FileUpload } from "primereact/fileupload";
import { SaveDialog } from "../../components/dialogs/SaveDialog";
import { useDialogState } from "../../components/dialogs/useDialogState";
import { Dialog } from "primereact/dialog";



export function TaskList({schema}:{schema:XEntity}){
    const columns = schema?.attributes?.filter(column => column.inList) ?? [];
    const stateManager = useDataTableStateManager(schema.defaultPageSize, columns,undefined )
    const {data,error,isLoading,mutate}= useTasks(encodeDataTableState(stateManager.state))
    const tableColumns = columns.map(x=>createColumn(x));
    const {handleErrorOrSuccess, CheckErrorStatus} = useCheckError();
    const {visible, showDialog, hideDialog} = useDialogState()

    async function handleAddExportTask(){
        const {error} = await addExportTask ();
        await handleErrorOrSuccess(error, 'Export task added!', mutate); 
    }

    async function handleImportDemoData(){
        const {error} = await  importDemoData();
        await handleErrorOrSuccess(error, 'Task added!', mutate);
    }
    
    function onAddImportTaskUpload(){
        mutate();
        hideDialog();
    }
    
    async function handleAddImportTask(){
        showDialog();
    }
    
    async function handleArchiveExportTask(id:number){
        const {error} = await archiveExportTask (id);
        await handleErrorOrSuccess(error, 'Export task added!', mutate);
    }

    const actionBodyTemplate = (item: SystemTask) => {
        if (item.type === 'export' && item.taskStatus==='finished'){
            return <>
                <a href={getExportTaskDownloadFileLink(item.id) }><Button >Download</Button></a>{' '}
                <Button onClick={()=>handleArchiveExportTask(item.id)}>Delete File</Button>
            </>
        }
        return <></>
    };
    
    tableColumns.push(
        <Column key={'action'} body={actionBodyTemplate} exportable={false} style={{minWidth: '12rem'}}></Column>
    )
    
    return <>
        <FetchingStatus isLoading={isLoading} error={error}/>
        <h2>{schema?.displayName} list</h2>
        <Button onClick={handleAddExportTask}>Add Export Task</Button>{' '}
        <Button onClick={handleAddImportTask}>Add Import Task</Button>{' '}
        <Button onClick={handleImportDemoData}>Import Demo Data</Button>
        <CheckErrorStatus/>
        <div className="card">
            {data && columns &&<EditDataTable columns={tableColumns} data={data} stateManager={stateManager}/>}
        </div>
        <Dialog maximizable visible={visible} 
                header={'Select a file to upload'} modal className="p-fluid" 
                onHide={hideDialog}>
            <FileUpload withCredentials mode={"basic"} auto url={getAddImportTaskUploadUrl()}  name={'files'} onUpload={onAddImportTaskUpload}/>
        </Dialog>
    </>
}