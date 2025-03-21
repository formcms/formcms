import {Button} from "primereact/button";
import {useDialogState} from "../../components/dialogs/useDialogState";
import {useEditTable} from "./useEditTable";
import {addCollectionItem, useCollectionData} from "../services/entity";
import {SaveDialog} from "../../components/dialogs/SaveDialog";
import {ItemForm} from "./ItemForm";
import {EditDataTable} from "../../components/data/EditDataTable";
import {XAttr, XEntity} from "../types/xEntity";
import {useDataTableStateManager} from "../../components/data/useDataTableStateManager";
import {encodeDataTableState} from "../../components/data/dataTableStateUtil";
import {createColumn} from "../../components/data/columns/createColumn";
import {getFileUploadURL} from "../services/asset";
import {useCheckError} from "../../components/useCheckError";
import {useNavigate} from "react-router-dom";


export function EditTable(
    {
        baseRouter,
        column, 
        data, 
        schema, 
        getFullAssetsURL
    }: {
        baseRouter: string,
        schema: XEntity,
        column: XAttr,
        data: any,
        getFullAssetsURL: (arg: string) => string
    }
) {
    //data
    const {id, listColumns, inputColumns, targetSchema} = useEditTable(data, schema, column)
    const stateManager = useDataTableStateManager(schema.primaryKey, 8, listColumns, "");
    const { data: collectionData, mutate } = useCollectionData(schema.name, id, column.field, encodeDataTableState(stateManager.state));
    const tableColumns = listColumns.map(x => 
        createColumn(x, getFullAssetsURL, x.field == schema.labelAttributeName ? onEdit : undefined));

    const formId = "edit-table" + column.field;

    //conponent
    const navigate = useNavigate();
    const {handleErrorOrSuccess, CheckErrorStatus} = useCheckError();
    const {visible, showDialog, hideDialog} = useDialogState()

    function onEdit(rowData: any) {
        var id = rowData[targetSchema!.primaryKey];
        const url = `${baseRouter}/${targetSchema!.name}/${id}?ref=${encodeURIComponent(window.location.href)}`;
        navigate(url);
    }

    async function onSubmit(formData: any) {
        const {error} = await addCollectionItem(schema.name, id, column.field, formData)
        await handleErrorOrSuccess(error, "submit success", () => {
            mutate();
            hideDialog();
        });
    }

    return <div className={'card col-12'}>
        <label id={column.field} className="font-bold">
            {column.header}
        </label><br/>
        <Button outlined label={'Add ' + column.header} onClick={showDialog} size="small"/>
        {' '}
        <EditDataTable dataKey={schema.primaryKey} 
                       columns={tableColumns} 
                       data={collectionData}
                       stateManager={stateManager}/>
        <SaveDialog
            formId={formId}
            visible={visible}
            handleHide={hideDialog}
            header={'Add ' + column.header}>
            <>
                <CheckErrorStatus/>
                <ItemForm
                    id={formId}
                    formId={formId}
                    uploadUrl={getFileUploadURL()}
                    columns={inputColumns}
                    data={{}}
                    getFullAssetsURL={getFullAssetsURL}
                    onSubmit={onSubmit}/>
            </>
        </SaveDialog>
    </div>
}