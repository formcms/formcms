import {deleteJunctionItems, saveJunctionItems, useJunctionData} from "../services/entity";
import {Button} from "primereact/button";
import {useCheckError} from "../../components/useCheckError";
import {useConfirm} from "../../components/useConfirm";
import {usePicklist} from "./usePicklist";
import {useDialogState} from "../../components/dialogs/useDialogState";
import {SelectDataTable} from "../../components/data/SelectDataTable";
import {SaveDialog} from "../../components/dialogs/SaveDialog";
import { XAttr, XEntity } from "../types/xEntity";
import { useDataTableStateManager } from "../../components/data/useDataTableStateManager";
import { encodeDataTableState } from "../../components/data/dataTableStateUtil";
import { createColumn } from "../../components/data/columns/createColumn";

export function Picklist({column, data, schema, getFullAssetsURL}: {
    data: any,
    column: XAttr,
    schema: XEntity,
    getFullAssetsURL : (arg:string) =>string
}) {
    const {visible, showDialog, hideDialog} = useDialogState()
    const {
        id, listColumns,
        existingItems, setExistingItems,
        toAddItems, setToAddItems
    } = usePicklist(data, schema, column)
    
    const tableColumns = listColumns.map(x=>createColumn(x,getFullAssetsURL));
    
    const existingStateManager= useDataTableStateManager(schema.primaryKey,10, listColumns,"");
    const {data: subgridData, mutate: subgridMutate} = useJunctionData(schema.name, id, column.field, false, 
        encodeDataTableState(existingStateManager.state));

    const excludedStateManager= useDataTableStateManager(schema.primaryKey,10, listColumns,"");
    const {data: excludedSubgridData, mutate: execMutate} = useJunctionData(schema.name, id, column.field, true,
        encodeDataTableState(excludedStateManager.state));
    
    const {handleErrorOrSuccess, CheckErrorStatus} = useCheckError();
    const {confirm,Confirm} = useConfirm("picklist" +column.field);
    
    const mutateDate = () => {
        setExistingItems(null);
        setToAddItems(null)
        subgridMutate()
        execMutate()

    }

    const handleSave = async () => {
        const {error} = await saveJunctionItems(schema.name, id, column.field, toAddItems)
        handleErrorOrSuccess(error, 'Save success', ()=> {
            mutateDate()
            hideDialog()
        })
    }

    const onDelete = async () => {
        confirm('Do you want to delete these item?', async () => {
            const {error} = await deleteJunctionItems(schema.name, id, column.field, existingItems)
            handleErrorOrSuccess(error, 'Delete Succeed', ()=> {
                mutateDate()
            })
        })
    }

    return <div className={'card col-12'}>
        <label id={column.field} className="font-bold">
            {column.header}
        </label><br/>
        <CheckErrorStatus/>
        <Confirm/>
        <Button outlined label={'Select ' + column.header} onClick={showDialog} size="small"/>
        {' '}
        <Button type={'button'} label={"Delete "} severity="danger" onClick={onDelete} outlined size="small"/>
        <SelectDataTable
            selectionMode={'multiple'}
            data={subgridData}
            columns={tableColumns}
            selectedItems={existingItems}
            setSelectedItems={setExistingItems}
            stateManager={existingStateManager}
        />
        <SaveDialog
            visible={visible}
            handleHide={hideDialog}
            handleSave={handleSave}
            header={'Select ' + column.header}>
            <SelectDataTable
                selectionMode={'multiple'}
                columns={tableColumns}
                data={excludedSubgridData}
                stateManager={excludedStateManager}
                
                selectedItems={toAddItems}
                setSelectedItems={setToAddItems}
            />
        </SaveDialog>
    </div>
}
