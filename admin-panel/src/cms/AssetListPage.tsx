import {SelectButton, SelectButtonChangeEvent} from "primereact/selectbutton";
import {EntityPageProps} from "../../lib/admin-panel-lib/cms/EntityRouter";
import {displayModes, useAssetList} from "../../lib/admin-panel-lib/cms/pages/useAssetListPage";


export function AssetListPage({schema,baseRouter}:EntityPageProps) {
    const {displayMode,setDisplayMode,AssetListPageMain} = useAssetList(baseRouter,schema);
    return <>
        <h2>{schema?.displayName} list</h2>
        <div className="flex gap-5 justify-between">
            <SelectButton
                value={displayMode}
                onChange={(e: SelectButtonChangeEvent) => setDisplayMode(e.value)}
                options={displayModes}
            />
        </div>
        <AssetListPageMain/>
    </>
}