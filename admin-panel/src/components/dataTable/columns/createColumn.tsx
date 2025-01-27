import {textColumn} from "./textColumn";
import {imageColumn} from "./imageColumn";
import {fileColumn} from "./fileColumn";
import {XAttr, XEntity } from "../xEntity";

export function createColumn(props:{
    schema:XEntity, 
    column:XAttr, 
    baseRouter:string, 
    getFullAssetsURL? : (arg:string) =>string |undefined }
) {
    switch (props.column.displayType){
        case 'image':
        case 'gallery':
            return imageColumn(props)
        case 'file':
            return fileColumn(props)
        default:
            return textColumn(props)
    }
}