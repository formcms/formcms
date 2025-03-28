import {Button} from "primereact/button";
import {userNewDataItemPage} from "../../lib/admin-panel-lib/cms/pages/userNewDataItemPage";
import {EntityPageProps} from "../../lib/admin-panel-lib/cms/EntityRouter";
import {GlobalStateKeys, useGlobalState} from "../globalState";

export function NewDataItemPage({schema,baseRouter}: EntityPageProps) {
    const {handleGoBack, formId,NewDataItemPageMain} = userNewDataItemPage(schema, baseRouter);
    const [lan] = useGlobalState<string>(GlobalStateKeys.Language, 'en');
    return (
        <>
            <br/>
            <Button label={lan === 'en' ? 'Save ' : '保存 ' + schema.displayName} type="submit" form={formId}
                    icon="pi pi-check"/>
            {' '}
            <Button type={'button'} label={lan === 'en' ? "Back" : '返回'} onClick={handleGoBack}/>
            <br/>
            <br/>
            <NewDataItemPageMain/>
        </>
    )
}