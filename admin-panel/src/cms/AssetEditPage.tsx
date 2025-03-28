import {Button} from "primereact/button";
import {FileUpload} from "primereact/fileupload";
import {useAssetEditPage} from "../../lib/admin-panel-lib/cms/pages/useAssetEditPage";
import {EntityPageProps} from "../../lib/admin-panel-lib/cms/EntityRouter";
import {GlobalStateKeys, useGlobalState} from "../globalState";

export function AssetEditPage({schema,baseRouter}:EntityPageProps) {
    const {
        asset,formId, replaceAssetUrl,
        handleDownload, handleUpload, handleDelete,
        FeaturedImage, AssetLinkTable, MetaDataForm
    } = useAssetEditPage(baseRouter,schema);
    const [lan] = useGlobalState<string>( GlobalStateKeys.Language, 'en');
    return <>
        <h3>{lan==='en'?'Edit ':'编辑'} {asset.name}</h3>
        <FeaturedImage/>
        <br/>
        {
            asset && <div className="mt-2 flex gap-4">
                <label className="block font-bold">File Name:</label>
                <label>{asset.name}</label>

                <label className="block font-bold">Type:</label>
                <label className="block">{asset.type}</label>

                <label className="block font-bold">Size:</label>
                <label>{asset.size}</label>

            </div>
        }
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
                url={replaceAssetUrl}
                onUpload={handleUpload}
                chooseLabel="Replace file"
                name={'files'}
            />
            <Button
                label={'Save Metadata'}
                type="submit"
                form={formId}
                icon="pi pi-check"/>
            <Button
                label={'Delete'}
                type="button"
                onClick={handleDelete}
                className="p-button-danger"
                icon="pi pi-remove"/>
        </div>
        <br/>
        <MetaDataForm/>
        <AssetLinkTable/>
    </>
}