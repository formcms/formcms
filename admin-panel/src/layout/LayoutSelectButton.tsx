import {GlobalStateKeys, useGlobalState} from "../globalState";
import {SelectButton} from "primereact/selectbutton";
import {CnLayoutOptions, EnLayoutOptions} from "../types/layout";

export function LayoutSelectButton() {
    const [value, setValue] = useGlobalState<string>( GlobalStateKeys.Layout, 'sidebar');
    const [language, _] = useGlobalState<string>( GlobalStateKeys.Language, 'en');

    return <SelectButton
        value={value}
        onChange={(e) => setValue(e.value)}
        options={language == 'en' ? EnLayoutOptions:CnLayoutOptions}
    />
}