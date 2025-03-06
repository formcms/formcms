import {LookupContainer} from "./LookupContainer";
import {TreeSelectContainer} from "./TreeSelectContainer";

import {TextInput} from "../../components/inputs/TextInput";
import {TextAreaInput} from "../../components/inputs/TextAreaInput";
import {EditorInput} from "../../components/inputs/EditorInput";
import {NumberInput} from "../../components/inputs/NumberInput";
import {DatetimeInput} from "../../components/inputs/DatetimeInput";
import {DateInput} from "../../components/inputs/DateInput";
import {FileInput} from "../../components/inputs/FileInput";
import {GalleryInput} from "../../components/inputs/GalleryInput";
import {DropDownInput} from "../../components/inputs/DropDownInput";
import {MultiSelectInput} from "../../components/inputs/MultiSelectInput";
import {XAttr } from "../types/xEntity";
import { AssetSelector } from "./AssetSelector";
import { MultiAssetSelector } from "./MultiAssetSelector";

export function createInput(props :{
    column: XAttr,
    data: any,id: any, 
    control: any, register: any,
    uploadUrl:string,
    getFullAssetsURL : (arg:string) =>string
}) {
    const {field, displayType,options} = props.column
    switch (displayType) {
        case 'text':
            return <TextInput className={'field col-12 md:col-4'} key={field} {...props}/>
        case 'textarea':
            return <TextAreaInput className={'field col-12  md:col-4'} key={field} {...props}/>
        case 'editor':
            return <EditorInput className={'field col-12'} key={field} {...props}/>
        case 'number':
            return <NumberInput className={'field col-12 md:col-4'} key={field} {...props}/>
        case 'datetime':
            return <DatetimeInput className={'field col-12  md:col-4'} inline={false} key={field} {...props}/>
        case 'date':
            return <DateInput className={'field col-12  md:col-4'} key={field} {...props}/>
        case 'image':
            return <FileInput  fileSelector={ AssetSelector} previewImage className={'field col-12  md:col-4'} key={field} {...props}/>
        case 'gallery':
            return <GalleryInput  fileSelector={AssetSelector} className={'field col-12  md:col-4'} key={field} {...props}/>
        case 'file':
            return <FileInput fileSelector={AssetSelector} download className={'field col-12  md:col-4'} key={field} {...props}/>
        case 'dropdown':
            return <DropDownInput options={props.column.options.split(',')} className={'field col-12 md:col-4'} key={field}{...props}/>
        case 'lookup':
            return <LookupContainer className={'field col-12 md:col-4'} key={field}{...props}/>
        case 'multiselect':
            return <MultiSelectInput options={(options??'').split(',')} className={'field col-12  md:col-4'} key={field} {...props}/>
        case 'treeSelect':
            return <TreeSelectContainer className={'field col-12  md:col-4'} key={field} {...props}/>
        default:
            return <TextInput className={'field col-12 md:col-4'} key={field} {...props}/>
    }
}

