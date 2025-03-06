import { createColumn } from "../../components/data/columns/createColumn";
import { encodeDataTableState } from "../../components/data/dataTableStateUtil";
import { useDataTableStateManager } from "../../components/data/useDataTableStateManager";
import { FetchingStatus } from "../../components/FetchingStatus";
import { useAssetEntity, useAssets, useGetCmsAssetsUrl } from "../services/asset";
import { XEntity } from "../types/xEntity";
import { SelectDataTable } from "../../components/data/SelectDataTable";
import { Dialog } from "primereact/dialog";
import { Button } from "primereact/button";

export function MultiAssetSelector(
    {
        show,
        setShow,
        paths,
        setPaths,
    }: {
        show:boolean
        setShow:(show: boolean) => void

        paths: string[]
        setPaths: (paths: string[]) => void
    }
) {
    var {data:assetEntity} = useAssetEntity();
    return assetEntity
        ?<MultiAssetSelectorComponent show={show} setShow={setShow} paths={paths}  schema={assetEntity} setPaths={setPaths}/>
        :<></>
}


export function MultiAssetSelectorComponent(
    {
        schema,
        
        show,
        setShow,
        paths,
        setPaths
    }: {
        schema:XEntity
        
        show:boolean
        setShow:(show: boolean) => void
        
        paths: string[]
        setPaths: (paths: string[]) => void
    }
){

    const columns = schema?.attributes?.filter(column => column.inList) ?? [];
    const stateManager = useDataTableStateManager(schema.defaultPageSize, columns)
    const {data,error,isLoading}= useAssets(encodeDataTableState(stateManager.state))
    const  getCmsAssetUrl= useGetCmsAssetsUrl();

    const tableColumns = columns.map(x=>createColumn(x, getCmsAssetUrl));

    return <>
        <Dialog maximizable
                visible={show}
                style={{width: '80%'}}
                modal className="p-fluid"
                onHide={()=>setShow(false)}
                footer={<Button type ='button' label={'Ok'} onClick={()=>setShow(false)}/>}
        >
            <FetchingStatus isLoading={isLoading} error={error}/>
            <h2>{schema?.displayName} list</h2>
            <div className="card">
                {data && columns &&  <SelectDataTable
                    dataKey={"path"}
                    columns={tableColumns}
                    data={data}
                    stateManager={stateManager}
                    selectedItems={paths.map(path=>({path}))}
                    setSelectedItems={(items:any[])=>setPaths(items.map(x=>x.path))}
                />}
            </div>
        </Dialog>
    </>
}