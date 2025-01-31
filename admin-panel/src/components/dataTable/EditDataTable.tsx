import {DataTable} from "primereact/datatable";
import {ListResponse} from "../../cms-client/types/listResponse";
import {Column} from "primereact/column";
import {useNavigate} from "react-router-dom";
import { Button } from "primereact/button";

export function EditDataTable(
    {
        columns, 
        data, 
        stateManager:{state,handlers:{onPage,onFilter,onSort}},
        onEdit,
        onDelete,
        onView,
    }:
    {
        columns: React.JSX.Element[];
        data: ListResponse | undefined
        stateManager:{
            state:any
            handlers:{
                onPage:any,
                onFilter:any,
                onSort:any,
            }
        }
        onEdit?:(rowData:any)=>void
        onDelete?:(rowData:any)=>void
        onView?:(rowData:any)=>void
    }) {
    const navigate = useNavigate();
    const actionBodyTemplate = (rowData: any) => {
        return (
            <>

                {onView &&
                    <Button icon="pi pi-eye" rounded outlined className="mr-2" onClick={() => onView(rowData)}/>}
                {onEdit &&
                    <Button icon="pi pi-pencil" rounded outlined className="mr-2" onClick={() => onEdit(rowData)}/>}
                {onDelete &&
                    <Button icon="pi pi-trash" rounded outlined severity="danger" onClick={() => onDelete(rowData)}/>}
            </>
        );
    };
  

    return columns && data && <DataTable
        sortMode="multiple"
        //dataKey={schema.primaryKey}
        value={data.items}
        paginator
        totalRecords={data.totalRecords}
        rows={state.rows}
        lazy
        first={state.first}
        filters={state.filters}
        multiSortMeta={state.multiSortMeta}
        sortField={state.sortField}
        sortOrder={state.sortOrder}
        onSort={onSort}
        onFilter={onFilter}
        onPage={onPage}>
        {columns}
        {(onEdit||onDelete||onView) &&
            <Column body={actionBodyTemplate} exportable={false} style={{minWidth: '12rem'}}></Column>}
    </DataTable>
}