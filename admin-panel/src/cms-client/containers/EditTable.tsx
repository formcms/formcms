import {Button} from "primereact/button";
import { useDialogState } from "../../components/dialogs/useDialogState";
import { useEditTable } from "./useEditTable";
import {addCollectionItem, useCollectionData} from "../services/entity";
import { SaveDialog } from "../../components/dialogs/SaveDialog";
import { ItemForm } from "./ItemForm";
import {Message} from "primereact/message";
import {Toast} from "primereact/toast";
import {useRef, useState} from "react";
import { EditDataTable } from "../../components/data/EditDataTable";
import { XAttr, XEntity } from "../types/xEntity";
import { useDataTableStateManager } from "../../components/data/useDataTableStateManager";
import { encodeDataTableState } from "../../components/data/dataTableStateUtil";
import { createColumn } from "../../components/data/columns/createColumn";
import { getFileUploadURL } from "../services/asset";


export function EditTable({column, data, schema, getFullAssetsURL}: {
    schema: XEntity,
    column: XAttr,
    data: any,
    getFullAssetsURL : (arg:string) =>string
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
    
    return  <div className={'card col-12'}>
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
                    uploadUrl={getFileUploadURL()}
                    columns={inputColumns}
                    data={{}}
                    getFullAssetsURL={getFullAssetsURL}
                    onSubmit={onSubmit}/>
                {error && error.split('\n').map(e => (<><Message severity={'error'} text={e}/>&nbsp;&nbsp;</>))}
            </>
        </SaveDialog>
    </div>
}
