import {useState} from "react";
import {XAttr, XEntity } from "../types/schemaExt";

export function usePicklist(data :any, schema:XEntity, column: XAttr )
{
    const id = (data ?? {})[schema?.primaryKey ?? '']
    const targetSchema = column.junction;
    const listColumns = targetSchema?.attributes?.filter((column: any) => column.inList ) ?? []; 
    
    const [existingItems, setExistingItems] = useState(null)
    const [toAddItems, setToAddItems] = useState(null)
    return {
        id, listColumns,
        existingItems, setExistingItems, 
        toAddItems, setToAddItems
    }
}