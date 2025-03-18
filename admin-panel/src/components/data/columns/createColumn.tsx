import {textColumn} from "./textColumn";
import {imageColumn} from "./imageColumn";
import {fileColumn} from "./fileColumn";
import {XAttr} from "../../xEntity";

export function createColumn(
    column:XAttr, 
    getFullAssetsURL? : (arg:string) =>string |undefined ,
    onClick?: (rowData:any) => void,
){
    var field = column.displayType == "lookup" || column.displayType === "treeSelect" 
        ? column.lookup!.labelAttributeName
        : column.field;
    switch (column.displayType){
        case 'image':
        case 'gallery':
            return imageColumn(field, column.header,getFullAssetsURL)
        case 'file':
            return fileColumn(field,column.header,getFullAssetsURL)
        default:
            return textColumn(field,column.header,column.displayType,onClick)
    }
}