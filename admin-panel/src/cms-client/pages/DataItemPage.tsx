import {useParams} from "react-router-dom";
import {ItemForm} from "../containers/ItemForm";
import {deleteItem, updateItem, useItemData, savePublicationSettings} from "../services/entity";
import {Divider} from "primereact/divider";
import {Button} from "primereact/button";
import {ButtonGroup} from 'primereact/buttongroup';

import {Picklist} from "../containers/Picklist";
import {useCheckError} from "../../components/useCheckError";
import {useConfirm} from "../../components/useConfirm";
import {PageLayout} from "./PageLayout";
import {FetchingStatus} from "../../components/FetchingStatus";
import {EditTable} from "../containers/EditTable";
import {TreeContainer} from "../containers/TreeContainer";
import {DisplayType, XEntity} from "../types/xEntity";
import {SaveDialog} from "../../components/dialogs/SaveDialog";
import {useDialogState} from "../../components/dialogs/useDialogState";
import {PublicationSettings} from "../containers/PublicationSettings";
import {DefaultAttributeNames} from "../types/defaultAttributeNames";
import {PublicationStatus} from "../types/publicationStatus";
import {SpecialQueryKeys} from "../types/specialQueryKeys";
import {getFileUploadURL, useGetCmsAssetsUrl} from "../services/asset";
import {DefaultColumnNames} from "../types/defaultColumnNames";

export function DataItemPage({baseRouter}: { baseRouter: string }) {
    const {schemaName} = useParams()
    return <PageLayout schemaName={schemaName ?? ''} baseRouter={baseRouter} page={DataItemPageComponent}/>
}

export function DataItemPageComponent(
    {
        schema,
        baseRouter
    }: {
        schema: XEntity,
        baseRouter: string
    }
) {
    //entrance 
    const {id} = useParams()
    const referingUrl = new URLSearchParams(location.search).get("ref");

    //data
    const {data, error: useItemError, isLoading, mutate} = useItemData(schema.name, id)
    const trees = schema.attributes.filter(x => x.displayType == DisplayType.Tree);


    //variable and state    
    const previewUrl = getPreviewUrl();
    const itemEditFormId = "editForm" + schema.name
    const scheduleFormId = "schedule" + schema.name
    const publishFormId = "publish" + schema.name

    const {visible: scheduleVisible, showDialog: showSchedule, hideDialog: hideSchedule} = useDialogState()
    const {visible: publishVisible, showDialog: showPublish, hideDialog: hidePublish} = useDialogState()


    //references
    const getCmsAssetUrl = useGetCmsAssetsUrl();
    const {confirm, Confirm} = useConfirm("dataItemPage" + schema.name);
    const {handleErrorOrSuccess: handlePageErrorOrSucess, CheckErrorStatus: PageErrorStatus} = useCheckError();
    const {handleErrorOrSuccess: handlePublish, CheckErrorStatus: CheckPublishErrorStatus} = useCheckError();
    const {handleErrorOrSuccess: handleSchedule, CheckErrorStatus: CheckScheduleErrorStatus} = useCheckError();

    function inputColumns() {
        return schema?.attributes?.filter(
            (x) => {
                return x.inDetail && !x.isDefault && x.displayType != 'picklist' && x.displayType != "tree" && x.displayType != 'editTable'
            }
        ) ?? [];
    }

    function tables() {
        return schema?.attributes?.filter(attr =>
            attr.displayType === DisplayType.Picklist
            || attr.displayType == DisplayType.EditTable) ?? []
    }

    function showUnpublish() {
        return data && (data[DefaultAttributeNames.PublicationStatus] === PublicationStatus.Published
            || data[DefaultAttributeNames.PublicationStatus] === PublicationStatus.Scheduled);
    }

    function getPreviewUrl() {
        if (!schema.previewUrl) return null;
        const connChar = schema.previewUrl.indexOf("?") > 0 ? "&" : "?";
        return `${schema.previewUrl}${id}${connChar}${SpecialQueryKeys.Preview}=1`;
    }

    async function onSubmit(formData: any) {
        formData[schema.primaryKey] = id
        formData[DefaultColumnNames.UpdatedAt] = data[DefaultColumnNames.UpdatedAt];

        const {error} = await updateItem(schema.name, formData)
        await handlePageErrorOrSucess(error, 'Save Succeed', mutate)
    }

    async function onDelete() {
        confirm('Do you want to delete this item?', async () => {
            const {error} = await deleteItem(schema.name, data)
            await handlePageErrorOrSucess(error, 'Delete Succeed', () => {
                window.location.href = referingUrl ?? `${baseRouter}/${schema.name}`
            });
        })
    }

    async function onPublish(formData: any) {
        formData[schema.primaryKey] = data[schema.primaryKey];
        formData[DefaultAttributeNames.PublicationStatus] = PublicationStatus.Published;

        const {error} = await savePublicationSettings(schema.name, formData)
        await handlePublish(error, 'Publish Succeed', () => {
            mutate();
            hidePublish();
        })
    }

    async function onUnpublish() {
        var formData: any = {}
        formData[schema.primaryKey] = data[schema.primaryKey];
        formData[DefaultAttributeNames.PublicationStatus] = PublicationStatus.Unpublished;

        const {error} = await savePublicationSettings(schema.name, formData)
        await handlePageErrorOrSucess(error, 'Publish Succeed', mutate)
    }

    async function onSchedule(formData: any) {
        formData[schema.primaryKey] = data[schema.primaryKey];
        formData[DefaultAttributeNames.PublicationStatus] = PublicationStatus.Scheduled;
        const {error} = await savePublicationSettings(schema.name, formData)
        await handleSchedule(error, 'Schedule Succeed', () => {
            mutate();
            hideSchedule();
        })
    }

    return <>
        <FetchingStatus isLoading={isLoading} error={useItemError}/>
        <Confirm/>

        <ButtonGroup>
            <Button type={'submit'} label={`Save ${schema.displayName}`} icon="pi pi-check" form={itemEditFormId}/>
            <Button type={'button'} label={`Delete ${schema.displayName}`} icon="pi pi-trash" severity="danger"
                    onClick={onDelete}/>
            {referingUrl && <Button type={'button'} label={"Back"} icon="pi pi-chevron-left"
                                    onClick={() => window.location.href = referingUrl}/>}
        </ButtonGroup>
        &nbsp;
        <ButtonGroup>
            <Button type={'button'} label={"Publish / Update Publish Time"} icon="pi pi-cloud" onClick={showPublish}/>
            <Button type={'button'} label={"Schedule / Reschedule"} icon="pi pi-calendar" onClick={showSchedule}/>
            {showUnpublish() && <Button type={'button'} label={"Unpublish"} icon="pi pi-ban" onClick={onUnpublish}/>}
        </ButtonGroup>
        &nbsp;
        {previewUrl && <Button type={'button'} label={"Preview"} onClick={() => window.location.href = previewUrl}/>}
        <div><PageErrorStatus/></div>

        {data &&
            <div className="grid">
                <div className={`col-12 md:col-12 lg:${trees.length > 0 ? "col-9" : "col-12"}`}>
                    <ItemForm uploadUrl={getFileUploadURL()} formId={itemEditFormId} columns={inputColumns()} {...{
                        schema,
                        data,
                        id,
                        onSubmit,
                        getFullAssetsURL: getCmsAssetUrl
                    }} />
                    {
                        tables().map((column) => {
                            const props = {schema, data, column, getFullAssetsURL: getCmsAssetUrl, baseRouter}
                            return <div key={column.field}>
                                <Divider/>
                                {column.displayType === 'picklist' && <Picklist key={column.field} {...props}/>}
                                {column.displayType === 'editTable' && <EditTable key={column.field} {...props}/>}
                            </div>
                        })
                    }
                </div>
                {trees.length > 0 && <div className="col-12 md:col-12 lg:col-3">
                    {
                        trees.map((column) => {
                            return <div key={column.field}>
                                <TreeContainer key={column.field} entity={schema} data={data}
                                               column={column}></TreeContainer>
                                <Divider/>
                            </div>
                        })
                    }
                </div>
                }
            </div>
        }
        <SaveDialog
            width={'50%'}
            formId={publishFormId}
            visible={publishVisible}
            handleHide={hidePublish}
            header={'Save '}>
            <CheckPublishErrorStatus/>
            <PublicationSettings onSubmit={onPublish} data={data} formId={publishFormId}/>
        </SaveDialog>

        <SaveDialog
            width={'50%'}
            formId={scheduleFormId}
            visible={scheduleVisible}
            handleHide={hideSchedule}
            header={'Save '}>
            <CheckScheduleErrorStatus/>
            <PublicationSettings onSubmit={onSchedule} data={data} formId={scheduleFormId}/>
        </SaveDialog>
    </>
}