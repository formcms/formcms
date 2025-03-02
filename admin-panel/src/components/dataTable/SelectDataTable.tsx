import {DataTable} from "primereact/datatable";
import {Column} from "primereact/column";
import {ListResponse} from "../../cms-client/types/listResponse";

export function SelectDataTable(
    {
        dataKey,
        columns,
        data,
        selectedItems, 
        setSelectedItems,
        actionBodyTemplate,
        stateManager: {state, handlers: {onPage, onFilter, onSort}},
    }:
    {
        columns: React.JSX.Element[];
        data: ListResponse | undefined
        selectedItems: any
        setSelectedItems: any
        actionBodyTemplate?: any
        stateManager: {
            state: any
            handlers: {
                onPage: any,
                onFilter: any,
                onSort: any,
            }
        }
        dataKey?: string;
    }
) {
    const {items, totalRecords} = data ?? {}
    return columns && data && <DataTable
        dataKey={dataKey}
        sortMode="multiple"
        value={items}
        paginator
        totalRecords={totalRecords}
        rows={state?.rows ?? 10}
        lazy
        first={state?.first}
        filters={state?.filters}
        multiSortMeta={state?.multiSortMeta}
        sortField={state?.sortField}
        sortOrder={state?.sortOrder}
        onSort={onSort}
        onFilter={onFilter}
        onPage={onPage}
        selection={selectedItems}
        onSelectionChange={(e) => setSelectedItems(e.value)}
    >
        <Column selectionMode="multiple" headerStyle={{width: '3rem'}}></Column>
        {columns}
        {actionBodyTemplate &&
            <Column body={actionBodyTemplate} exportable={false} style={{minWidth: '12rem'}}></Column>}
    </DataTable>
}