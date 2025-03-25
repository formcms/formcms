import {Button} from "primereact/button";
import {useDataListPage} from "../../lib/admin-panel-lib/cms/pages/useDataListPage";
import {EntityPageProps} from "../../lib/admin-panel-lib/cms/EntityRouter";

export function DataListPage({schema,baseRouter}:EntityPageProps) {
    const {createNewItem, DataListPageMain} = useDataListPage(schema,baseRouter);
    return <>
        <h2>{schema.displayName} list</h2>
        <Button onClick={createNewItem}>Create New {schema.displayName}</Button>
        <DataListPageMain/>
    </>
}