import {Button} from "primereact/button";
import {userNewDataItemPage} from "../../lib/admin-panel-lib/cms/pages/userNewDataItemPage";
import {EntityPageProps} from "../../lib/admin-panel-lib/cms/EntityRouter";

export function NewDataItemPage({schema,baseRouter}: EntityPageProps) {
    const {handleGoBack, formId,NewDataItemPageMain} = userNewDataItemPage(schema, baseRouter);
    return (
        <>
            <Button label={'Save ' + schema.displayName} type="submit" form={formId}  icon="pi pi-check"/>
            {' '}
            <Button type={'button'} label={"Back"}  onClick={handleGoBack}/>
            <NewDataItemPageMain/>
        </>
    )
}