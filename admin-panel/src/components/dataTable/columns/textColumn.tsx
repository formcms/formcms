import {Column} from "primereact/column";
import { XAttr } from "../xEntity";

export function textColumn(
    column: XAttr,
    onClick?: (rowData:any) => void,
) {
    let field = (column.displayType == "lookup" || column.displayType === "treeSelect") 
        ? column.field + "." + column.lookup!.labelAttributeName
        : column.field;

    var colType = column.displayType == 'number' 
        ? 'numeric'
        : (column.displayType == 'datetime' || column.displayType == 'date') 
            ? 'date'
            : 'text';
    
    const bodyTemplate = (item: any) => {
        let val = item[column.field]
        if (val) {
            if (column.displayType === "lookup" || column.displayType === "treeSelect") {
                val = val[column.lookup!.labelAttributeName]
            } else if (column.displayType === 'multiselect') {
                val = val.join(", ")
            }
        }
        return onClick?<a href={"#"} onClick={()=>onClick(item)}>{val}</a>:<>{val}</>
    };
    
    return <Column
        dataType={colType}
        key={column.field}
        field={field}
        header={column.header}
        sortable filter body={bodyTemplate}>
    </Column>
}