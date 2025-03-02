import {Dialog} from "primereact/dialog";
import {createColumn} from "../../components/dataTable/columns/createColumn";
import {encodeDataTableState} from "../../components/dataTable/dataTableStateUtil";
import {EditDataTable} from "../../components/dataTable/EditDataTable";
import {useDataTableStateManager} from "../../components/dataTable/useDataTableStateManager";
import {FetchingStatus} from "../../components/FetchingStatus";
import {useAssetEntity, useAssets, useGetCmsAssetsUrl} from "../services/asset";
import {XEntity} from "../types/xEntity"

export function AssetSelector(
    {
        onSelect,
        show,
        setShow,
    }: {
        show: boolean;
        setShow: (show: boolean) => void
        onSelect: (selectedPath: string) => void
    }
) {
    var {data: assetEntity} = useAssetEntity();
    return assetEntity ?
        <AssetSelectorComponent show={show} setShow={setShow} schema={assetEntity} onSelect={onSelect}/> : <></>
}

export function AssetSelectorComponent(
    {
        show,
        setShow,
        schema,
        onSelect
    }: {
        show: boolean;
        setShow: (show: boolean) => void
        schema: XEntity
        onSelect: (selectedPath: string) => void
    }
) {
    const columns = schema?.attributes?.filter(column => column.inList) ?? [];
    const stateManager = useDataTableStateManager(schema.defaultPageSize, columns, undefined)
    const {data, error, isLoading} = useAssets(encodeDataTableState(stateManager.state))
    const getCmsAssetUrl = useGetCmsAssetsUrl();
    function handleSelect(rowData:any) {
        onSelect(rowData.path);
    }

    const tableColumns = columns.map(x => createColumn(x, getCmsAssetUrl, handleSelect));
    return <Dialog maximizable
                   visible={show}
                   style={{width: '80%'}}
                   modal className="p-fluid"
                   onHide={() => setShow(false)}>
        <FetchingStatus isLoading={isLoading} error={error}/>
        <h2>{schema?.displayName} list</h2>
        <div className="card">
            {data && columns && <EditDataTable columns={tableColumns} data={data} stateManager={stateManager}/>}
        </div>
    </Dialog>
}