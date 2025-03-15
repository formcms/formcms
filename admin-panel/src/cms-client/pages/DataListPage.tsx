import {Link, useLocation, useNavigate, useParams } from "react-router-dom";
import {useListData,deleteItem} from "../services/entity";
import {Button} from "primereact/button";
import {PageLayout} from "./PageLayout";
import {FetchingStatus} from "../../components/FetchingStatus";
import {EditDataTable} from "../../components/data/EditDataTable";
import {useEffect} from "react";
import {XEntity} from "../types/xEntity";
import { useDataTableStateManager } from "../../components/data/useDataTableStateManager";
import { encodeDataTableState } from "../../components/data/dataTableStateUtil";
import { createColumn } from "../../components/data/columns/createColumn";
import { useCheckError } from "../../components/useCheckError";
import { useConfirm } from "../../components/useConfirm";
import { useGetCmsAssetsUrl } from "../services/asset";

export function DataListPage({baseRouter}:{baseRouter:string}){
    const {schemaName} = useParams()
    return <PageLayout schemaName={schemaName??''} baseRouter={baseRouter} page={DataListPageComponent}/>
}

export const NewItemRoute = "new";
export function DataListPageComponent({schema,baseRouter}:{schema:XEntity,baseRouter:string}) {
    const getCmsAssetUrl=useGetCmsAssetsUrl();
    const navigate = useNavigate();
    const {handleErrorOrSuccess, CheckErrorStatus} = useCheckError();
    const {confirm, Confirm} = useConfirm("dataItemPage" + schema.name);
    

    const columns = schema?.attributes?.filter(x => 
        x.inList &&  x.displayType != 'picklist' && x.displayType != "tree" && x.displayType != 'editTable') ?? [];
    const dataTableColumns = columns.map(x=>createColumn(x, getCmsAssetUrl, x.field==schema.labelAttributeName?onEdit:undefined))
    
    const initQs = useLocation().search.replace("?","");
    const stateManager = useDataTableStateManager(schema.defaultPageSize, columns, initQs );
    const qs = encodeDataTableState(stateManager.state);
    const {data, error, isLoading,mutate}= useListData(schema.name,qs)
    useEffect(()=> window.history.replaceState(null,"", `?${qs}`),[stateManager.state]);
    


    function onDuplicate  (rowData:any) {
        var id = rowData[schema.primaryKey];
        const url =`${baseRouter}/${schema.name}/${NewItemRoute}?sourceId=${id}&ref=${encodeURIComponent(window.location.href)}`;
        navigate(url);
    }
    
    function onEdit(rowData:any){
        var id = rowData[schema.primaryKey];
        const url =`${baseRouter}/${schema.name}/${id}?ref=${encodeURIComponent(window.location.href)}`;
        navigate(url);
    }

    async function onDelete (rowData:any) {
        confirm(`Do you want to delete this item [${rowData[schema.labelAttributeName]}]?`, async () => {
            const {error} = await deleteItem(schema.name, rowData);
            await handleErrorOrSuccess(error, 'Delete Succeed', mutate);
        })
    }
    
    return <>
        <FetchingStatus isLoading={isLoading} error={error}/>
        <h2>{schema.displayName} list</h2>
        <Link to={NewItemRoute}><Button>Create New {schema.displayName}</Button></Link>
        <CheckErrorStatus/>
        <Confirm/>
        <div className="card">
            {data &&
                <EditDataTable 
                    columns={dataTableColumns} 
                    data={data} 
                    stateManager={stateManager} 
                    onEdit={onEdit}
                    onDelete={onDelete}
                    onDuplicate={onDuplicate}
                />
            }
        </div>
    </>
}
