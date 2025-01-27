import {DataTable} from "primereact/datatable";
import {createColumn} from "./columns/createColumn";
import {XAttr, XEntity} from "../../cms-client/types/schemaExt";
import {ListResponse} from "../../cms-client/types/listResponse";

export function LazyDataTable({baseRouter, schema, columns, data, lazyState, eventHandlers, getFullAssetsURL}: {
    schema: XEntity|undefined,
    columns: XAttr[],
    baseRouter: string
    data: ListResponse | undefined
    lazyState: any
    eventHandlers: any
    getFullAssetsURL?: (arg: string) => string | undefined
}) {
    return columns && data && schema && <DataTable
        sortMode="multiple"
        dataKey={schema.primaryKey}
        value={data.items}
        paginator
        totalRecords={data.totalRecords}
        rows={lazyState.rows}
        lazy
        first={lazyState.first}
        filters={lazyState.filters}
        multiSortMeta={lazyState.multiSortMeta}
        sortField={lazyState.sortField}
        sortOrder={lazyState.sortOrder}
        {...eventHandlers}
    >
        {
            columns.map(
                (column) => createColumn(
                    {column, schema, getFullAssetsURL, baseRouter}
                )
            )
        }
    </DataTable>
}