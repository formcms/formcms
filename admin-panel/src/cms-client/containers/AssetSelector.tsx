import {Dialog} from "primereact/dialog";
import {createColumn} from "../../components/data/columns/createColumn";
import {encodeDataTableState} from "../../components/data/dataTableStateUtil";
import {useDataTableStateManager} from "../../components/data/useDataTableStateManager";
import {FetchingStatus} from "../../components/FetchingStatus";
import {useAssetEntity, useAssets, useGetCmsAssetsUrl} from "../services/asset";
import {XEntity} from "../types/xEntity"
import {SelectButton, SelectButtonChangeEvent} from "primereact/selectbutton";
import {useState} from "react";
import {Asset} from "../types/asset";
import {SelectDataTable} from "../../components/data/SelectDataTable";
import { GalleryView } from "../../components/data/GalleryView";
import { AssetField } from "../types/assetUtils";



type AssetSelectorProps = {

    show: boolean;
    setShow: (show: boolean) => void;
    
    path?:string;
    setPath?:(path:string) => void;

    paths?: string[];
    setPaths?: (paths: string[]) => void;
};

export function AssetSelector(
    props: AssetSelectorProps
) {
    var {data: assetEntity} = useAssetEntity();
    return assetEntity ?
        <AssetSelectorComponent schema={assetEntity} {...props} /> : <></>
}

enum DisplayMode {
    'List' = 'List',
    'Gallery' = 'Gallery',
}

export function AssetSelectorComponent(
    {
        schema,

        show,
        setShow,

        path,
        setPath,
        
        paths,
        setPaths,
    }: AssetSelectorProps & {
        schema: XEntity
    }
) {
    const modes: DisplayMode[] = [DisplayMode.List, DisplayMode.Gallery];
    const [mode, setMode] = useState<DisplayMode>(modes[0]);

    const columns = schema?.attributes?.filter(column => column.inList) ?? [];
    const stateManager = useDataTableStateManager(schema.defaultPageSize, columns, undefined)
    const {data, error, isLoading} = useAssets(encodeDataTableState(stateManager.state),false)
    const getCmsAssetUrl = useGetCmsAssetsUrl();

    const tableColumns = columns.map(x => createColumn(x, getCmsAssetUrl, undefined));
    
    const handleSetSelectItems=(item:any)=>{
        console.log("handleSetSelectItems",item);
        if (setPath){
            setPath(item.path);
            setShow(false);
        } 
        
        if (setPaths) {
            setPaths(item.map((x: Asset) => x.path));
        }
    }
    
    
    return <Dialog maximizable
                   header={<>
                       <h2>{schema?.displayName} List</h2>
                       <div className="card flex justify-content-center">
                           <SelectButton value={mode} 
                                         onChange={(e: SelectButtonChangeEvent) => setMode(e.value)}
                                         options={modes}/>
                       </div>
                   </>}
                   visible={show}
                   style={{width: '80%'}}
                   modal className="p-fluid"
                   onHide={() => setShow(false)}>
        <FetchingStatus isLoading={isLoading} error={error}/>
        <div className="card">
            {
                data && columns && mode === DisplayMode.List &&
                <SelectDataTable
                    selectionMode={path? 'single' : 'multiple'}
                    dataKey={"path"}
                    columns={tableColumns}
                    data={data}
                    stateManager={stateManager}
                    selectedItems={path? {path}:paths?.map(path => ({path}))}
                    setSelectedItems={handleSetSelectItems}
                />
            }
            {
                data && columns && mode === DisplayMode.Gallery &&
                <GalleryView 
                    state={stateManager.state} 
                    onPage={stateManager.handlers.onPage}
                    data={data} 
                    path={path} 
                    setPath={(path)=> handleSetSelectItems({path})} 
                    setPaths={setPaths} 
                    paths={paths} 
                    getAssetUrl={getCmsAssetUrl}
                    nameField={AssetField('name')}
                    pathField={AssetField('path')}
                    titleField={AssetField('title')}
                />
            }
        </div>
    </Dialog>
}