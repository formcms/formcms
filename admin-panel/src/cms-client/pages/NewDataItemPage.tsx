import {ItemForm} from "../containers/ItemForm";
import {addItem, useItemData} from "../services/entity";
import {Button} from "primereact/button";
import {fileUploadURL, getFullCmsAssetUrl} from "../configs";
import {useCheckError} from "../../components/useCheckError";
import {useParams} from "react-router-dom";
import {PageLayout} from "./PageLayout";
import { XEntity } from "../types/xEntity";

export function NewDataItemPage({baseRouter}:{baseRouter:string}) {
    const {schemaName} = useParams()
    return <PageLayout schemaName={schemaName??''} baseRouter={baseRouter} page={NewDataItemPageComponent}/>
}

export function NewDataItemPageComponent({schema,baseRouter}:{schema:XEntity, baseRouter:string }) {
    const id =  new URLSearchParams(location.search).get("sourceId");
    const {data} = useItemData(schema.name, id)

    const referingUrl = new URLSearchParams(location.search).get("ref");

    const {handleErrorOrSuccess, CheckErrorStatus} = useCheckError();
    const formId = "newForm" + schema.name
    const uploadUrl = fileUploadURL()
    const inputColumns = schema?.attributes?.filter(
        x =>{
            return x.inDetail &&!x.isDefault&& x.displayType != "editTable" && x.displayType != "tree" &&x.displayType != 'picklist';
        }
    ) ??[];
    const onSubmit = async (formData: any) => {
        const {data, error} = await addItem(schema.name, formData)
        handleErrorOrSuccess(error, 'Save Succeed', ()=> {
            window.location.href = `${baseRouter}/${schema.name}/${data[schema.primaryKey]}`;
        })
    }

    return <>
        <Button label={'Save ' + schema.displayName} type="submit" form={formId}  icon="pi pi-check"/>
        {' '}
        {referingUrl &&<Button type={'button'} label={"Back"}  onClick={()=>window.location.href = referingUrl}/>}

        <CheckErrorStatus/>
        {(!id || data) && <ItemForm columns={inputColumns} {...{data:data??{} , onSubmit,  formId,uploadUrl,  getFullAssetsURL:getFullCmsAssetUrl}}/>}
    </>
}