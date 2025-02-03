import {Button} from "primereact/button";
import { useDialogState } from "../../components/dialogs/useDialogState";
import { useEditTable } from "./useEditTable";
import {addCollectionItem, useCollectionData} from "../services/entity";
import { SaveDialog } from "../../components/dialogs/SaveDialog";
import { ItemForm } from "./ItemForm";
import { fileUploadURL } from "../configs";
import {Message} from "primereact/message";
import {Toast} from "primereact/toast";
import {useRef, useState} from "react";
import { EditDataTable } from "../../components/dataTable/EditDataTable";
import { XAttr, XEntity } from "../types/xEntity";
import { useDataTableStateManager } from "../../components/dataTable/useDataTableStateManager";
import { encodeDataTableState } from "../../components/dataTable/dataTableStateUtil";
import { createColumn } from "../../components/dataTable/columns/createColumn";


export function EditTable({baseRouter,column, data, schema, getFullAssetsURL}: {
    schema: XEntity,
    column: XAttr,
    data: any,
    getFullAssetsURL : (arg:string) =>string
    baseRouter:string
}) {
    const {visible, showDialog, hideDialog} = useDialogState()
    const { id, listColumns, inputColumns} = useEditTable(data, schema, column)
    const tableColumns = listColumns.map(x => createColumn(x, getFullAssetsURL));
    const formId = "edit-table" + column.field;
    const toastRef = useRef<any>(null);
    const [error, setError] = useState('')
    
    const stateManager= useDataTableStateManager(10, listColumns,"");
    const {data:collectionData,mutate} = useCollectionData(schema.name, id, column.field, encodeDataTableState(stateManager.state));
    
    const onSubmit =async (formData: any) => {
        const {error} = await addCollectionItem(schema.name,id,column.field,formData)
        if (!error){
            hideDialog();
            toastRef.current.show({severity: 'info', summary:"success"})
            setError("");
            mutate();
        }else {
            setError(error);
        }
    }
    
    return <div className={'card col-12'}>
        <Toast ref={toastRef} position="top-right" />
        <label id={column.field} className="font-bold">
            {column.header}
        </label><br/>
        <Button outlined label={'Add ' + column.header} onClick={showDialog} size="small"/>
        {' '}
        <EditDataTable columns={tableColumns} data={collectionData} stateManager={stateManager}/>
        <SaveDialog
            formId={formId}
            visible={visible}
            handleHide={hideDialog}
            header={'Add ' + column.header}>
            <>
                <ItemForm
                    id={undefined}
                    formId={formId}
                    uploadUrl={fileUploadURL()}
                    columns={inputColumns}
                    data={{}}
                    getFullAssetsURL={getFullAssetsURL}
                    onSubmit={onSubmit}/>
                {error && error.split('\n').map(e => (<><Message severity={'error'} text={e}/>&nbsp;&nbsp;</>))}
            </>
        </SaveDialog>
    </div>
}
