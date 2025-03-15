import { Dialog } from "primereact/dialog";
import { XEntity } from "../types/xEntity";
import { ArrayToObject } from "../../components/inputs/DictionaryInputUtils";
import { AssetField } from "../types/assetUtils";
import {updateAssetMeta, useAssetEntity, useGetCmsAssetsUrl, useSingleAssetByPath } from "../services/asset";
import { FetchingStatus } from "../../components/FetchingStatus";
import { useCheckError } from "../../components/useCheckError";
import { useForm } from "react-hook-form";
import { createInput } from "./createInput";
import { Button } from "primereact/button";

type AssetMetaDataDialogProps = {
    path: string,
    show: boolean;
    setShow: (show: boolean) => void;
}

export function AssetMetadataEditor(
    props: AssetMetaDataDialogProps
) {
    var {data: assetEntity} = useAssetEntity();
    return assetEntity 
        ? <AssetMetadataEditorComponent schema={assetEntity} {...props} /> 
        : <></>
}

export function AssetMetadataEditorComponent(
    {
        path,
        show,
        setShow,
        schema,

    }: AssetMetaDataDialogProps & {schema: XEntity}
) {
    const formId = "AssetMetaDataDialog" + schema.name
    const getCmsAssetUrl = useGetCmsAssetsUrl();

    const {
        register,
        handleSubmit,
        control,
        reset,
        
    } = useForm()

    const {data, isLoading, error,mutate} = useSingleAssetByPath(show ? path: null);
    const {handleErrorOrSuccess, CheckErrorStatus} = useCheckError();

    const handleClose =()=>{
        reset();
        setShow(false);
    }
    const onSubmit = async (formData: any) => {
        var payload = {
            ...formData,
            metadata: ArrayToObject(formData[AssetField('metadata')]),
            id:data?.id,
        }
        const {error} = await updateAssetMeta(payload)
        await handleErrorOrSuccess(error, 'Save Meta Data Succeed', ()=>{
            mutate();
            handleClose();
        })
    }
    const columns = schema?.attributes?.filter(
        x => {
            return x.inDetail && !x.isDefault && x.displayType != "editTable" && x.displayType != "tree" && x.displayType != 'picklist';
        }
    ) ?? [];
    return  <Dialog maximizable
                    header={'Edit Metadata'}
                    visible={show}
                    style={{width: '80%'}}
                    modal className="p-fluid"
                    onHide={handleClose}>
        <FetchingStatus isLoading={isLoading} error={error}/>
        <CheckErrorStatus/>
        {data && <form onSubmit={handleSubmit(onSubmit)} id={formId}>
            <Button
                label={'Save'}
                type="submit"
                form={formId}
                style={{width: '100px'}}
                icon="pi pi-check"/> 
            <div className="formgrid grid">
                {
                    columns.map((column: any) => createInput({
                        data,
                        column,
                        register,
                        control,
                        id:column.field,
                        getFullAssetsURL: getCmsAssetUrl,
                        uploadUrl: ''
                    }))
                }
            </div>
        </form>
        }
    </Dialog>
}