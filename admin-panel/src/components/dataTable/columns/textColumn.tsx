import {Column} from "primereact/column";
import {Link} from "react-router-dom";
import { XAttr, XEntity } from "../../../cms-client/types/schemaExt";

export function textColumn({column, baseRouter, schema}:{
    baseRouter:string
    schema:XEntity
    column: XAttr,
}){
    let field = column.field;
    if (column.displayType == "lookup" || column.displayType === "treeSelect"){
        field = column.field + "." + column.lookup!.labelAttributeName;
    }
    var colType = 'text';
    switch (column.displayType){
        case 'number':
            colType = 'numeric';
            break;
        case 'datetime':
        case 'date':
            colType = 'date';
            break;
    }

    const bodyTemplate = (item:any) => {
        let val = item[column.field]
        if (val) {
            if (column.displayType === "lookup" || column.displayType === "treeSelect") {
                val = val[column.lookup!.labelAttributeName]
            }else if (column.displayType === 'multiselect'){
                val = val.join(", ")
            }
        }

        if (column.field == schema.labelAttributeName){
            return <Link to={`${baseRouter}/${schema.name}/${item[schema.primaryKey]}?ref=${encodeURIComponent(window.location.href)}`}>{val}</Link>
        }else {
            return <>{val}</>
        }
    };
    return <Column 
        dataType={colType} 
        key={column.field} 
        field={field} 
        header={column.header} 
        sortable filter body={bodyTemplate}>
    </Column>
}