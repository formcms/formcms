import {useParams} from "react-router-dom"
import {getAssetReplaceUrl, useGetCmsAssetsUrl, useSingleAsset} from "../services/asset";
import {XEntity} from "../types/xEntity";
import {useForm} from "react-hook-form";
import {createInput} from "../containers/createInput";
import {Button} from "primereact/button";
import {FetchingStatus} from "../../components/FetchingStatus";
import {FileUpload} from "primereact/fileupload";
import { Image } from 'primereact/image';
import { formatFileSize } from "../types/assetUtils";
import { useState } from "react";

export function AssetEdit(
    {
        schema,
    }: {
        schema: XEntity,
    }
) {
    const [version, setVersion] = useState(1);
    const {id} = useParams()
    const {data, isLoading, error} = useSingleAsset(id);
    const getCmsAssetUrl = useGetCmsAssetsUrl();
    const columns = schema?.attributes?.filter(
        x => {
            return x.inDetail && !x.isDefault && x.displayType != "editTable" && x.displayType != "tree" && x.displayType != 'picklist';
        }
    ) ?? [];
    const {
        register,
        handleSubmit,
        control
    } = useForm()

    const onSubmit = async (formData: any) => {
        console.log(formData)

    }


    const handleDownload = () => {
        if (data && data.path) {
            const url = getCmsAssetUrl(data.path);
            const link = document.createElement('a');
            link.href = url;
            link.download = data.name || 'asset'; // Fallback to 'asset' if name is not available
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        }
    }
    const formId = schema.name
    return data && columns && <>
        <FetchingStatus isLoading={isLoading} error={error}/>
        <br/>
        {data?.type?.startsWith("image") &&
        <div className="card flex justify-content-start">
            <Image src={getCmsAssetUrl(data.path + `?version=${version}`)} indicatorIcon={<i className="pi pi-search"></i>} alt="Image" preview width="400" />
        </div>
        }
        <br/>
        <div className="mt-2 flex gap-4">
            <label className="block font-bold">Type:</label>
            <label className="block">{data.type}</label>
            
            <label className="block font-bold">Size:</label>
            <label>{formatFileSize(data.size)}</label>
        </div>
        <br/>
        <div style={{display: "flex", gap: "10px", alignItems: "center"}}>
            <Button
                label="Download"
                icon="pi pi-download"
                onClick={handleDownload}
                className="p-button-secondary"
            />
            <FileUpload
                withCredentials
                mode={"basic"}
                auto
                url={getAssetReplaceUrl(data.path)}
                onUpload={(e) => {
                    setVersion(x=>x+1);
                }}
                chooseLabel="Replace file"
                name={'files'}
            />
            <Button
                label={'Save Asset Metadata'}
                type="submit"
                form={formId}
                icon="pi pi-check"/>
        </div>
        <form onSubmit={handleSubmit(onSubmit)} id={formId}>
            <div className="formgrid grid">
                {
                    columns.map((column: any) => createInput({
                        data,
                        column,
                        register,
                        control,
                        id,
                        getFullAssetsURL: getCmsAssetUrl,
                        uploadUrl: ''
                    }))
                }
            </div>
        </form>
    </>
}