import {SelectButton} from "primereact/selectbutton";
import {GlobalStateKeys, useGlobalState} from "../globalState";
import {LanguageOptions} from "../types/language";

export function LanguageSelectButton() {
    const [value, setValue] = useGlobalState<string>( GlobalStateKeys.Language, 'en');

    return <SelectButton
       value={value}
       onChange={(e) => setValue(e.value)}
       options={LanguageOptions}
    />
}