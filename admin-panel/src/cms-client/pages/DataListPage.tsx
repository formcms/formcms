import {Link, useLocation, useParams } from "react-router-dom";
import {useListData} from "../services/entity";
import {Button} from "primereact/button";
import {getFullCmsAssetUrl} from "../configs";
import {PageLayout} from "./PageLayout";
import {FetchingStatus} from "../../components/FetchingStatus";
import {LazyDataTable} from "../../components/dataTable/LazyDataTable";
import {useEffect} from "react";
import {XEntity} from "../types/schemaExt";
import { useLazyStateHandlers } from "../../components/dataTable/useLazyStateHandlers";
import { encodeLazyState } from "../../components/dataTable/lazyStateUtil";

export function DataListPage({baseRouter}:{baseRouter:string}){
    const {schemaName} = useParams()
    return <PageLayout schemaName={schemaName??''} baseRouter={baseRouter} page={DataListPageComponent}/>
}

export function DataListPageComponent({schema,baseRouter}:{schema:XEntity,baseRouter:string}) {
    const columns = schema?.attributes?.filter(x => x.inList &&  x.displayType != 'picklist' && x.displayType != "tree" && x.displayType != 'editTable') ?? [];
    let {lazyState, eventHandlers} = useLazyStateHandlers(schema.defaultPageSize, columns, useLocation().search.replace("?",""))
    var qs = encodeLazyState(lazyState);
    const {data, error, isLoading}= useListData(schema.name,qs)
    useEffect(()=> window.history.replaceState(null,"", `?${qs}`),[lazyState]);

    return <>
        <FetchingStatus isLoading={isLoading} error={error}/>
        <h2>{schema.displayName} list</h2>
        <Link to={"new"}><Button>Create New {schema.displayName}</Button></Link>
        <div className="card">
            {data &&<LazyDataTable {...{columns,schema,baseRouter,data, eventHandlers, lazyState,  getFullAssetsURL:getFullCmsAssetUrl}}/>}
        </div>
    </>
}
