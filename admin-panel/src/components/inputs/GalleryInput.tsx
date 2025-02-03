import React from "react";
import {FileUpload} from "primereact/fileupload";
import {InputText} from "primereact/inputtext";
import {InputPanel} from "./InputPanel";
import {Galleria} from "primereact/galleria";

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
const itemTemplate = (item:any) => {
    return <img src={item.itemImageSrc} style={{ width: '100%' }} />
}
export function GalleryInput(props: {
    data: any,
    column: { field: string, header: string },
    register: any
    className: any
    control: any
    id: any
    uploadUrl: any
    getFullAssetsURL : (arg:string) =>string

}) {
    return <InputPanel  {...props} childComponent={(field: any) => {
        const urls = field.value??[];
        const items = urls.map((x:any) =>({
            itemImageSrc:props.getFullAssetsURL(x), thumbnailImageSrc:props.getFullAssetsURL(x)
        }));
        return <>
            <InputText type={'hidden'} id={field.name} value={field.value} className={' w-full'}
                       onChange={(e) => field.onChange(e.target.value)}/>

            <Galleria showIndicators responsiveOptions={responsiveOptions} numVisible={5} style={{ maxWidth: '50%'}}
                      item={itemTemplate}
                      showThumbnails={false}
                      value={items}/>
            <FileUpload withCredentials multiple mode={"basic"} auto url={props.uploadUrl}
                        onUpload={(e) => {
                field.onChange(e.xhr.responseText.split(','))
            }} name={'files'}/>
        </>
    }}/>
}