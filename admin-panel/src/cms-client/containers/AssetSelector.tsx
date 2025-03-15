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
import { Button } from "primereact/button";
import { GallerySelector } from "../../components/data/GallerySelector";



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
const displayModes: DisplayMode[] = [DisplayMode.List, DisplayMode.Gallery];

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
    const [displayMode, setDisplayMode] = useState<DisplayMode>(displayModes[0]);

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
                   header={schema?.displayName + "List"}
                   visible={show}
                   style={{width: '80%'}}
                   modal className="p-fluid"
                   onHide={() => setShow(false)}>
        <div className="flex gap-5 justify-between">
            <Button icon={'pi pi-check'} onClick={()=>setShow(false)} style={{width: '70px'}}>OK</Button>
            <SelectButton
                value={displayMode}
                onChange={(e: SelectButtonChangeEvent) => setDisplayMode(e.value)}
                options={displayModes}
            />
        </div>
       
        <FetchingStatus isLoading={isLoading} error={error}/>
        <div className="card">
            {
                data && columns && displayMode === DisplayMode.List &&
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
                /*single select*/
                data && columns && displayMode === DisplayMode.Gallery && setPath &&
                <GalleryView 
                    state={stateManager.state} 
                    onPage={stateManager.handlers.onPage}
                    data={data} 
                    path={path} 
                    onSelect={(asset:any)=> handleSetSelectItems(asset)} 
                    getAssetUrl={getCmsAssetUrl}
                    nameField={AssetField('name')}
                    pathField={AssetField('path')}
                    titleField={AssetField('title')}
                />
            }
            {
                /*multiple select*/
                data && columns && displayMode === DisplayMode.Gallery && setPaths &&
                <GallerySelector
                    paths={paths}
                    setPaths={setPaths}
                    state={stateManager.state}
                    onPage={stateManager.handlers.onPage}
                    data={data}
                    getAssetUrl={getCmsAssetUrl}
                    nameField={AssetField('name')}
                    pathField={AssetField('path')}
                    titleField={AssetField('title')}
                />
            }
        </div>
    </Dialog>
}