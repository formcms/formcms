import {ItemForm} from "../containers/ItemForm";
import {addItem} from "../services/entity";
import {Button} from "primereact/button";
import {fileUploadURL, getFullCmsAssetUrl} from "../configs";
import {useCheckError} from "../../components/useCheckError";
import {useParams} from "react-router-dom";
import {PageLayout} from "./PageLayout";
import { XEntity } from "../types/schemaExt";

export function NewDataItemPage({baseRouter}:{baseRouter:string}) {
    const {schemaName} = useParams()
    return <PageLayout schemaName={schemaName??''} baseRouter={baseRouter} page={NewDataItemPageComponent}/>
}

export function NewDataItemPageComponent({schema,baseRouter}:{schema:XEntity, baseRouter:string }) {
    const {handleErrorOrSuccess, CheckErrorStatus} = useCheckError();
    const formId = "newForm" + schema.name
    const uploadUrl = fileUploadURL()
    const inputColumns = schema?.attributes?.filter(
        (x: any) =>{
            return x.inDetail &&!x.isDefault&& x.dataType != "Junction" && x.dataType != "Collection" ;
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
        <CheckErrorStatus/>
        <ItemForm columns={inputColumns} {...{data:{}, onSubmit,  formId,uploadUrl,  getFullAssetsURL:getFullCmsAssetUrl}}/>
    </>
}