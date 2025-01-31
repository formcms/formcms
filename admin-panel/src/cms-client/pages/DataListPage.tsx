import {Link, useLocation, useNavigate, useParams } from "react-router-dom";
import {useListData,deleteItem} from "../services/entity";
import {Button} from "primereact/button";
import {getFullCmsAssetUrl} from "../configs";
import {PageLayout} from "./PageLayout";
import {FetchingStatus} from "../../components/FetchingStatus";
import {EditDataTable} from "../../components/dataTable/EditDataTable";
import {useEffect} from "react";
import {XEntity} from "../types/schemaExt";
import { useDataTableStateManager } from "../../components/dataTable/useDataTableStateManager";
import { encodeDataTableState } from "../../components/dataTable/dataTableStateUtil";
import { createColumn } from "../../components/dataTable/columns/createColumn";
import { useCheckError } from "../../components/useCheckError";
import { useConfirm } from "../../components/useConfirm";

export function DataListPage({baseRouter}:{baseRouter:string}){
    const {schemaName} = useParams()
    return <PageLayout schemaName={schemaName??''} baseRouter={baseRouter} page={DataListPageComponent}/>
}

export function DataListPageComponent({schema,baseRouter}:{schema:XEntity,baseRouter:string}) {
    const navigate = useNavigate();

    const columns = schema?.attributes?.filter(x => 
        x.inList &&  x.displayType != 'picklist' && x.displayType != "tree" && x.displayType != 'editTable') ?? [];
    const stateManager = useDataTableStateManager(schema.defaultPageSize, columns, useLocation().search.replace("?",""))
    
    const qs = encodeDataTableState(stateManager.state);
    const {data, error, isLoading,mutate}= useListData(schema.name,qs)

    const {handleErrorOrSuccess, CheckErrorStatus} = useCheckError();
    const {confirm, Confirm} = useConfirm("dataItemPage" + schema.name);

    const onEdit = (rowData:any)=>{
        const url =`${baseRouter}/${schema.name}/${rowData[schema.primaryKey]}?ref=${encodeURIComponent(window.location.href)}`;
        navigate(url);
    }

    const onDelete = async (rowData:any) => {
        confirm(`Do you want to delete this item [${rowData[schema.labelAttributeName]}]?`, async () => {
            const {error} = await deleteItem(schema.name, rowData);
            await handleErrorOrSuccess(error, 'Delete Succeed', mutate);
        })
    }

    const dataTableColumns = columns.map(x=>createColumn(x,getFullCmsAssetUrl, x.field==schema.labelAttributeName?onEdit:undefined))
    useEffect(()=> window.history.replaceState(null,"", `?${qs}`),[stateManager.state]);
    
    return <>
        <FetchingStatus isLoading={isLoading} error={error}/>
        <h2>{schema.displayName} list</h2>
        <Link to={"new"}><Button>Create New {schema.displayName}</Button></Link>
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
                />
            }
        </div>
    </>
}
