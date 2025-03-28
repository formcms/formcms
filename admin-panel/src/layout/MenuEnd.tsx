import UserAvatarDropdown from "./UserAvatarDropDown";
import {LanguageSelectButton} from "./LanguageSelectButton";
import {LayoutSelectButton} from "./LayoutSelectButton";

export function MenuEnd() {
    return <div className={'flex gap-3'}>
        <LanguageSelectButton/>
        <LayoutSelectButton/>
        <UserAvatarDropdown/>
    </div>
}