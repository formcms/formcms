import {Button} from "primereact/button";
import {useDataListPage} from "../../lib/admin-panel-lib/cms/pages/useDataListPage";
import {EntityPageProps} from "../../lib/admin-panel-lib/cms/EntityRouter";
import {GlobalStateKeys, useGlobalState} from "../globalState";

export function DataListPage({schema, baseRouter}: EntityPageProps) {
    const {createNewItem, DataListPageMain} = useDataListPage(schema, baseRouter);
    const [layout] = useGlobalState<string>(GlobalStateKeys.Layout, 'sidebar');
    const [lan] = useGlobalState<string>(GlobalStateKeys.Language, 'en');

    return <>
        {layout !== "sidebar" ? <h3>{schema.displayName} list</h3>:<br/>}
        <Button onClick={createNewItem}>{lan === 'en' ? 'Create New' : '新建'} {schema.displayName}</Button>
        <DataListPageMain/>
    </>
}