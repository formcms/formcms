import {textColumn} from "./textColumn";
import {imageColumn} from "./imageColumn";
import {fileColumn} from "./fileColumn";
import {XAttr} from "../xEntity";

export function createColumn(
    column:XAttr, 
    getFullAssetsURL? : (arg:string) =>string |undefined ,
    onClick?: (rowData:any) => void,
){
    switch (column.displayType){
        case 'image':
        case 'gallery':
            return imageColumn(column,getFullAssetsURL)
        case 'file':
            return fileColumn(column,getFullAssetsURL)
        default:
            return textColumn(column,onClick)
    }
}