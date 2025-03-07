import {Column} from "primereact/column";
import {createColumn} from "../../components/data/columns/createColumn";
import {encodeDataTableState} from "../../components/data/dataTableStateUtil";
import {EditDataTable} from "../../components/data/EditDataTable";
import {useDataTableStateManager} from "../../components/data/useDataTableStateManager";
import {FetchingStatus} from "../../components/FetchingStatus";
import {deleteAsset, useAssets, useGetCmsAssetsUrl} from "../services/asset";
import {XEntity} from "../types/xEntity";
import {AssetField} from "../types/assetUtils";
import {useConfirm} from "../../components/useConfirm";
import {useCheckError} from "../../components/useCheckError";
import {useNavigate} from "react-router-dom";

export function AssetList(
    {
        baseRouter,
        schema
    }: {
        baseRouter: string,
        schema: XEntity
    }
) {
    const navigate = useNavigate();
    const columns = schema?.attributes?.filter(column => column.inList) ?? [];
    const stateManager = useDataTableStateManager(schema.defaultPageSize, columns, undefined)
    const {data, error, isLoading, mutate} = useAssets(encodeDataTableState(stateManager.state), true)
    const getCmsAssetUrl = useGetCmsAssetsUrl();
    const tableColumns = columns.map(x => createColumn(x, getCmsAssetUrl, undefined));
    tableColumns.push(<Column key={AssetField("linkCount")} field={AssetField("linkCount")} header={"Link Count"}/>);
    const {confirm, Confirm} = useConfirm("dataItemPage" + schema.name);
    const {handleErrorOrSuccess, CheckErrorStatus} = useCheckError();

    const onEdit = (rowData: any) => {
        var id = rowData[schema.primaryKey];
        const url = `${baseRouter}/${id}?ref=${encodeURIComponent(window.location.href)}`;
        console.log(url);
        navigate(url);
    }
    const canDelete = (rowData: any) => {
        const ret = (rowData[AssetField("linkCount")] ?? 0) === 0;
        return ret;

    }
    const onDelete = async (rowData: any) => {

        confirm(`Do you want to delete this item [${rowData[schema.labelAttributeName]}]?`, async () => {
            const {error} = await deleteAsset(rowData[AssetField('id')]);
            await handleErrorOrSuccess(error, 'Delete Succeed', mutate);
        })
    }
    // const {handleErrorOrSuccess, CheckErrorStatus} = useCheckError();
    // const {visible, showDialog, hideDialog} = useDialogState() 
    return <>
        <FetchingStatus isLoading={isLoading} error={error}/>
        <CheckErrorStatus key={'AssetList'}/>
        <h2>{schema?.displayName} list</h2>
        <div className="card">
            {data && columns &&
                <EditDataTable 
                    columns={tableColumns} 
                    data={data} 
                    stateManager={stateManager} 
                    canDelete={canDelete} 
                    onDelete={onDelete}
                    onEdit={onEdit}
                />
            }
        </div>
        <Confirm/>
    </>
}