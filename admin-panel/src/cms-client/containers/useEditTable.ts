import {XAttr, XEntity } from "../types/xEntity";

export function useEditTable(data :any, schema:XEntity, column: XAttr )
{
    const id = (data ?? {})[schema.primaryKey ?? '']
    const targetSchema = column.collection;
    
    const listColumns = targetSchema?.attributes?.filter(
        (x) =>{
            return x.inList
                && x.displayType != 'picklist' && x.displayType != "tree" && x.displayType != 'editTable'
        }
    ) ?? [];
    
    const inputColumns = targetSchema?.attributes?.filter(
        x =>{
            return x.inDetail && !x.isDefault
                &&  x.displayType != 'picklist' && x.displayType != "tree" && x.displayType != 'editTable'
        }
    ) ??[];

    return {
        id, listColumns, inputColumns, targetSchema
    }
}