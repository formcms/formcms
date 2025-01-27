import {deleteJunctionItems, saveJunctionItems, useJunctionData} from "../services/entity";
import {Button} from "primereact/button";
import {useCheckError} from "../../components/useCheckError";
import {useConfirm} from "../../components/useConfirm";
import {usePicklist} from "./usePicklist";
import {useDialogState} from "../../components/dialogs/useDialogState";
import {SelectDataTable} from "../../components/dataTable/SelectDataTable";
import {SaveDialog} from "../../components/dialogs/SaveDialog";
import { XAttr } from "../types/schemaExt";
import { useLazyStateHandlers } from "../../components/dataTable/useLazyStateHandlers";
import { encodeLazyState } from "../../components/dataTable/lazyStateUtil";

export function Picklist({baseRouter,column, data, schema, getFullAssetsURL}: {
    data: any,
    column: XAttr,
    schema: any
    getFullAssetsURL : (arg:string) =>string
    baseRouter:string
}) {
    const {visible, showDialog, hideDialog} = useDialogState()
    const {
        id, targetSchema, listColumns,
        existingItems, setExistingItems,
        toAddItems, setToAddItems
    } = usePicklist(data, schema, column)
    
    const {lazyState ,eventHandlers}= useLazyStateHandlers(10, listColumns,"");
    const {data: subgridData, mutate: subgridMutate} = useJunctionData(schema.name, id, column.field, false, encodeLazyState(lazyState));

    const {lazyState :excludedLazyState,eventHandlers:excludedEventHandlers}= useLazyStateHandlers(10, listColumns,"");
    const {data: excludedSubgridData, mutate: execMutate} = useJunctionData(schema.name, id, column.field, true,excludedLazyState)
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
            data={subgridData}
            columns={listColumns}
            schema={targetSchema}
            selectedItems={existingItems}
            setSelectedItems={setExistingItems}
            lazyState={lazyState}
            eventHandlers={eventHandlers}
            getFullAssetsURL={getFullAssetsURL}
            baseRouter={baseRouter}
        />
        <SaveDialog
            visible={visible}
            handleHide={hideDialog}
            handleSave={handleSave}
            header={'Select ' + column.header}>
            <SelectDataTable
                schema={targetSchema}
                getFullAssetsURL={getFullAssetsURL}
                data={excludedSubgridData}
                columns={listColumns}
                selectedItems={toAddItems}
                setSelectedItems={setToAddItems}
                lazyState={excludedLazyState}
                eventHandlers={excludedEventHandlers}
                baseRouter={baseRouter}
            />
        </SaveDialog>
    </div>
}
