import {useNavigate, useParams} from "react-router-dom"
import {getAssetReplaceUrl, updateAssetMeta, useGetCmsAssetsUrl, useSingleAsset} from "../services/asset";
import {XEntity} from "../types/xEntity";
import {useForm} from "react-hook-form";
import {createInput} from "../containers/createInput";
import {Button} from "primereact/button";
import {FetchingStatus} from "../../components/FetchingStatus";
import {FileUpload} from "primereact/fileupload";
import { Image } from 'primereact/image';
import {AssetField, AssetLinkField, formatFileSize } from "../types/assetUtils";
import { useState } from "react";
import { useCheckError } from "../../components/useCheckError";
import { DataTable } from "primereact/datatable";
import { Column } from "primereact/column";
import { AssetLink } from "../types/asset";
import { ArrayToObject } from "../../components/inputs/DictionaryInputUtils";

export function AssetEdit(
    {
        baseRouter,
        schema,
    }: {
        baseRouter: string;
        schema: XEntity,
    }
) {
    const navigate = useNavigate();
    const [version, setVersion] = useState(1);
    const {id} = useParams()
    const {data, isLoading, error,mutate} = useSingleAsset(id);
    const getCmsAssetUrl = useGetCmsAssetsUrl();
    const {handleErrorOrSuccess, CheckErrorStatus} = useCheckError();

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
        var payload = {
            ...formData,
            metadata: ArrayToObject(formData[AssetField('metadata')]),
            id:data?.id,
        }
        const {error} = await updateAssetMeta(payload)
        await handleErrorOrSuccess(error, 'Save Meta Data Succeed', mutate) 
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
    const formId = "AssetEdit" + schema.name
    const actionBodyTemplate = (rowData: AssetLink) => {
        return ( <Button icon="pi pi-eye" rounded outlined className="mr-2" onClick={() => navigate(`${baseRouter}/${rowData.entityName}/${rowData.recordId}`)}/>);
    };
    return data && columns && <>
        <FetchingStatus isLoading={isLoading} error={error}/>
        <CheckErrorStatus/> 
        <br/>
        {data?.type?.startsWith("image") &&
        <div className="card flex justify-content-start">
            <Image src={getCmsAssetUrl(data.path + `?version=${version}`)} indicatorIcon={<i className="pi pi-search"></i>} alt="Image" preview width="400" />
        </div>
        }
        <br/>
        <div className="mt-2 flex gap-4">
            <label className="block font-bold">File Name:</label>
            <label>{data.name}</label>
            
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
                url={getAssetReplaceUrl(data.id)}
                onUpload={(e) => {
                    setVersion(x=>x+1);
                    mutate();
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
                    columns.map(column  => createInput({
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
        {data.links && <h3>Used By:</h3>}
        {data.links && <DataTable value={data.links} tableStyle={{ minWidth: '50rem' }}>
            <Column field={AssetLinkField('entityName')} header={'Entity Name'} ></Column>
            <Column field={AssetLinkField('recordId')} header={'Record Id'}></Column>
            <Column field={AssetLinkField('createdAt')}  header={'Created At'}></Column>
            <Column body={actionBodyTemplate} style={{minWidth: '12rem'}}></Column>
        </DataTable>}
    </>
}