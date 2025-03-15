import React, { useState } from "react";
import {FileUpload, FileUploadUploadEvent} from "primereact/fileupload";
import {InputText} from "primereact/inputtext";
import {InputPanel} from "./InputPanel";
import {Galleria} from "primereact/galleria";
import {Button} from "primereact/button";

const responsiveOptions = [
    {
        breakpoint: '991px',
        numVisible: 4
    },
    {
        breakpoint: '767px',
        numVisible: 3
    },
    {
        breakpoint: '575px',
        numVisible: 1
    }
];
const itemTemplate = (item: any) => {
    return <img src={item.itemImageSrc} style={{width: '100%'}}/>
}

export function GalleryInput(props: {
    data: any,
    column: { field: string, header: string },
    register: any
    className: any
    control: any
    id: any
    uploadUrl: any
    getFullAssetsURL: (arg: string) => string
    fileSelector?: React.ComponentType<{
        show: boolean;
        setShow:(show: boolean) => void
        
        setPaths: (selectedPath: string[]) => void
        paths: string[]
    }>
    metadataEditor?: React.ComponentType<{
        path:string;
        show: boolean;
        setShow: (show: boolean) => void;
    }>
}) {
    const FileSelectDialog = props.fileSelector;
    const [showChooseLib,setShowChooseLib] = useState(false)

    const MetadataEditor = props.metadataEditor;
    const [showMetadataEditor,setShowMetadataEdtior] = useState(false)
    
    return <InputPanel  {...props} childComponent={(field: any) => {
        const [activeIndex, setActiveIndex] = useState(0)
        const paths:string[] = field.value ?? [];
        const setPaths =(newPaths:string[])=> {
            setActiveIndex(0);
            field.onChange(newPaths);
        }
        const urls = paths.map((x: any) => ({
            itemImageSrc: props.getFullAssetsURL(x), thumbnailImageSrc: props.getFullAssetsURL(x)
        }));
        
        const handleRemoveActive= ()=>
        {
            var newPath =paths.filter((_, index) => index !== activeIndex); 
            setPaths(newPath);
        }
        
        const handleUploaded =(e:FileUploadUploadEvent) => {
            var newPath =[...paths, ...e.xhr.responseText.split(',')];
            setPaths(newPath);
        }
            

        return <>
            <InputText type={'hidden'} id={field.name} value={field.value} className={' w-full'}
                       onChange={(e) => field.onChange(e.target.value)}/>

            <Galleria showIndicators
                      activeIndex={activeIndex}
                      onItemChange={(e) => setActiveIndex(e.index)}
                      responsiveOptions={responsiveOptions} 
                      numVisible={5} 
                      style={{maxWidth: '50%'}}
                      item={itemTemplate}
                      showThumbnails={false}
                      value={urls}
            />
            <div style={{display: "flex", gap: "10px", alignItems: "center"}}>
                <FileUpload withCredentials
                            auto
                            multiple 
                            mode={"basic"} 
                            url={props.uploadUrl}
                            onUpload={handleUploaded}
                            name={'files'}
                            chooseLabel="Upload"
                />
                {
                    FileSelectDialog && (
                    <Button type='button'
                            icon={'pi pi-database'}
                            label="Choose"
                            onClick={()=>setShowChooseLib(true)}
                            className="p-button " // Match FileUpload styling
                    />)
                }
                {
                    MetadataEditor && paths.length > 0 &&(
                        <Button type='button'
                                icon={'pi pi-pencil'}
                                label="Edit"
                                onClick={()=>setShowMetadataEdtior(true)}
                                className="p-button " // Match FileUpload styling
                        />
                    )
                }
                <Button type='button'
                        icon={'pi pi-trash'}
                        label="Delete"
                        onClick={handleRemoveActive}
                        className="p-button " // Match FileUpload styling
                />
            </div>
            {
                FileSelectDialog && 
                <FileSelectDialog 
                    setPaths={setPaths} 
                    paths={paths} 
                    show={showChooseLib}
                    setShow={setShowChooseLib}
                />
            }
            {
                MetadataEditor && (
                    <MetadataEditor
                        show={showMetadataEditor}
                        setShow={setShowMetadataEdtior}
                        path ={paths[activeIndex]}
                    />
                )
            }
        </>
    }}/>
}