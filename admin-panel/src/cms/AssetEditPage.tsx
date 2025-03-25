import {Button} from "primereact/button";
import {FileUpload} from "primereact/fileupload";
import {useAssetEditPage} from "../../lib/admin-panel-lib/cms/pages/useAssetEditPage";
import {EntityPageProps} from "../../lib/admin-panel-lib/cms/EntityRouter";

export function AssetEditPage({schema,baseRouter}:EntityPageProps) {
    const {
        formId, replaceAssetUrl,
        handleDownload, handleUpload, handleDelete,
        FeaturedImage, AssetLinkTable, MetaDataForm,FileInfo
    } = useAssetEditPage(baseRouter,schema);
    return <>
        <FeaturedImage/>
        <br/>
        <FileInfo/>
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