import {ItemForm} from "../containers/ItemForm";
import {addItem, useItemData} from "../services/entity";
import {Button} from "primereact/button";
import {useCheckError} from "../../components/useCheckError";
import {useParams} from "react-router-dom";
import {PageLayout} from "./PageLayout";
import {DisplayType, XEntity } from "../types/xEntity";
import { getFileUploadURL, useGetCmsAssetsUrl } from "../services/asset";
import { ArrayToObject } from "../../components/inputs/DictionaryInputUtils";

export function NewDataItemPage({baseRouter}:{baseRouter:string}) {
    const {schemaName} = useParams()
    return <PageLayout schemaName={schemaName??''} baseRouter={baseRouter} page={NewDataItemPageComponent}/>
}

export function NewDataItemPageComponent({schema,baseRouter}:{schema:XEntity, baseRouter:string }) {
    //entrance and data
    const id =  new URLSearchParams(location.search).get("sourceId");
    const {data} = useItemData(schema.name, id)

    //ui state
    const formId = "newForm" + schema.name
    
    //navigate
    const getFullAssetsURL = useGetCmsAssetsUrl();
    const referingUrl = new URLSearchParams(location.search).get("ref");
    const uploadUrl = getFileUploadURL()

    //error
    const {handleErrorOrSuccess, CheckErrorStatus} = useCheckError();
    
    function inputColumns () {
        return schema?.attributes?.filter(
            x => {
                return x.inDetail && !x.isDefault && x.displayType != "editTable" && x.displayType != "tree" && x.displayType != 'picklist';
            }
        ) ?? [];
    }
    
    const onSubmit = async (formData: any) => {
        schema.attributes.filter(x=>x.displayType == DisplayType.Dictionary).forEach(a=>{
            formData[a.field] = ArrayToObject(formData[a.field]);
        });
        
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
        {(!id || data) && <ItemForm columns={inputColumns()} {...{data:data??{} , onSubmit,  formId,uploadUrl,  getFullAssetsURL}}/>}
    </>
}