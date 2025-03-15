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
import {useLocation, useNavigate} from "react-router-dom";
import {useEffect, useState} from "react";
import {GalleryView} from "../../components/data/GalleryView";
import {SelectButton, SelectButtonChangeEvent} from "primereact/selectbutton";

enum DisplayMode {
    'List' = 'List',
    'Gallery' = 'Gallery',
}

const displayModes: DisplayMode[] = [DisplayMode.List, DisplayMode.Gallery];

export function AssetList(
    {
        baseRouter,
        schema
    }: {
        baseRouter: string,
        schema: XEntity
    }
) {
    //entrance
    const location = useLocation();
    const initDisplayMode = new URLSearchParams(location.search).get("displayMode");
    const initQs = location.search.replace("?", "");

    //data
    const columns = schema?.attributes?.filter(column => column.inList && column.field !== AssetField('linkCount')) ?? [];
    const stateManager = useDataTableStateManager(schema.defaultPageSize, columns, initQs);
    const qs = encodeDataTableState(stateManager.state);
    const {data, error, isLoading, mutate} = useAssets(qs, true)

    //state
    const [displayMode, setDisplayMode] = useState<DisplayMode>(initDisplayMode as DisplayMode ?? displayModes[0]);

    //navigate
    useEffect(() => window.history.replaceState(null, "", `?displayMode=${displayMode}&${qs}`), [stateManager.state, displayMode]);

    //ref
    const getCmsAssetUrl = useGetCmsAssetsUrl();
    const navigate = useNavigate();
    const {confirm, Confirm} = useConfirm("dataItemPage" + schema.name);
    const {handleErrorOrSuccess, CheckErrorStatus} = useCheckError();

    function tableColumns() {
        const cols = columns.map(x =>
            createColumn(x, getCmsAssetUrl, x.field === AssetField("title") ? onEdit : undefined)
        );
        cols.push(<Column key={AssetField("linkCount")} field={AssetField("linkCount")} header={"Link Count"}/>);
        return cols;
    }

    const onEdit = (rowData: any) => {
        var id = rowData[schema.primaryKey];
        const url = `${baseRouter}/${id}?ref=${encodeURIComponent(window.location.href)}`;
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

    return <>
        <FetchingStatus isLoading={isLoading} error={error}/>
        <CheckErrorStatus key={'AssetList'}/>
        <h2>{schema?.displayName} list</h2>
        <div className="flex gap-5 justify-between">
            <SelectButton
                value={displayMode}
                onChange={(e: SelectButtonChangeEvent) => setDisplayMode(e.value)}
                options={displayModes}
            />
        </div>
        <div className="card">
            {data && columns && displayMode === DisplayMode.List &&
                <EditDataTable
                    dataKey={schema.primaryKey}
                    columns={tableColumns()}
                    data={data}
                    stateManager={stateManager}
                    canDelete={canDelete}
                    onDelete={onDelete}
                    onEdit={onEdit}
                />
            }
            {
                data && columns && displayMode === DisplayMode.Gallery &&
                <GalleryView
                    onSelect={onEdit}
                    state={stateManager.state}
                    onPage={stateManager.handlers.onPage}
                    data={data}
                    getAssetUrl={getCmsAssetUrl}

                    pathField={AssetField('path')}
                    nameField={AssetField('name')}
                    titleField={AssetField('title')}
                    typeField={AssetField('type')}
                />
            }
        </div>
        <Confirm/>
    </>
}