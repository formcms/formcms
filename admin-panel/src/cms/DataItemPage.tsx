import {ButtonGroup} from "primereact/buttongroup";
import {Button} from "primereact/button";
import {useDataItemPage} from "../../lib/admin-panel-lib/cms/pages/useDataItemPage";
import {EntityPageProps} from "../../lib/admin-panel-lib/cms/EntityRouter";

export function DataItemPage(
    {
        schema,
        baseRouter
    }: EntityPageProps
) {
    const {
        formId, showUnpublish, previewUrl,
        deleteProps:{handleDelete, ConfirmDelete, CheckDeleteStatus},
        handleGoBack,
        publishProps:{handleShowPublish, PublishDialog},
        scheduleProps:{handleShowSchedule, ScheduleDialog},
        unpublishProps:{onUnpublish, CheckUnpublishStatus},
        DataItemPageMain,
    } = useDataItemPage(schema, baseRouter);

    return <>
        <ButtonGroup>
            <Button type={'submit'} label={`Save ${schema.displayName}`} icon="pi pi-check" form={formId}/>
            <Button type={'button'} label={`Delete ${schema.displayName}`} icon="pi pi-trash" severity="danger"
                    onClick={handleDelete}/>
            <Button type={'button'} label={"Back"} icon="pi pi-chevron-left" onClick={handleGoBack}/>
        </ButtonGroup>
        &nbsp;
        <ButtonGroup>
            <Button type={'button'} label={"Publish / Update Publish Time"} icon="pi pi-cloud"
                    onClick={handleShowPublish}/>
            <Button type={'button'} label={"Schedule / Reschedule"} icon="pi pi-calendar" onClick={handleShowSchedule}/>
            {showUnpublish && <Button type={'button'} label={"Unpublish"} icon="pi pi-ban" onClick={onUnpublish}/>}
        </ButtonGroup>
        &nbsp;
        {previewUrl && <Button type={'button'} label={"Preview"} onClick={() => window.location.href = previewUrl}/>}
        <br/>
        <CheckDeleteStatus/>
        <CheckUnpublishStatus/>
        <DataItemPageMain/>
        <ConfirmDelete/>
        <PublishDialog/>
        <ScheduleDialog/>
    </>
}