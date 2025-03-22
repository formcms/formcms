import {textColumn} from "./textColumn";
import {imageColumn} from "./imageColumn";
import {fileColumn} from "./fileColumn";
import {DisplayType, XAttr} from "../../xEntity";
import {toDateStr, toDatetimeStr, utcStrToDatetimeStr} from "../../formatter";

const formmater :any = {
    [DisplayType.Datetime]: toDatetimeStr,
    [DisplayType.LocalDatetime]: utcStrToDatetimeStr,
    [DisplayType.Date]: toDateStr,
    [DisplayType.Multiselect]: (x:any)=>x.join(','),
    [DisplayType.Dictionary]: (x:any)=> Object.entries(x).map(([k,v])=>(`${k}:${v}`)).join(', ')
}

export function createColumn(
    column: XAttr,
    getFullAssetsURL?: (arg: string) => string | undefined,
    onClick?: (rowData: any) => void,
) {
    var field = column.displayType == "lookup" || column.displayType === "treeSelect"
        ? column.lookup!.name + "." + column.lookup!.labelAttributeName
        : column.field;

    var colType: 'numeric' | 'date' | 'text' = column.displayType == 'number'
        ? 'numeric'
        : (column.displayType == 'datetime' || column.displayType == 'date' || column.displayType === 'localDatetime')
            ? 'date'
            : 'text';

    switch (column.displayType) {
        case 'image':
        case 'gallery':
            return imageColumn(field, column.header, getFullAssetsURL)
        case 'file':
            return fileColumn(field, column.header, getFullAssetsURL)
        default:
            return textColumn(field, column.header, formmater[column.displayType], colType, onClick)
    }
}