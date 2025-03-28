import {SelectButton, SelectButtonChangeEvent} from "primereact/selectbutton";
import {EntityPageProps} from "../../lib/admin-panel-lib/cms/EntityRouter";
import {
    DisplayMode,
    DisplayModeOption, displayModeOptions,
    useAssetList
} from "../../lib/admin-panel-lib/cms/pages/useAssetListPage";
import {GlobalStateKeys, useGlobalState} from "../globalState";


export function AssetListPage({schema,baseRouter}:EntityPageProps) {
    const [lan] = useGlobalState<string>( GlobalStateKeys.Language, 'en');
    const {displayMode,setDisplayMode,AssetListPageMain} = useAssetList(baseRouter,schema);
    const cnDisplayModeOptions: DisplayModeOption[] = [
        {
            value: DisplayMode.List,
            label: '列表'
        },

        {
            value: DisplayMode.Gallery,
            label: '缩略图'
        }
    ];
    return <>
        <br/>
        <div className="flex gap-5 justify-between">
            <SelectButton
                value={displayMode}
                onChange={(e: SelectButtonChangeEvent) => setDisplayMode(e.value)}
                options={lan === 'en' ? displayModeOptions:cnDisplayModeOptions}
            />
        </div>
        <AssetListPageMain/>
    </>
}