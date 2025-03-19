import {Column} from "primereact/column";
import {DisplayType } from "../../xEntity";
import {toDateStr, toDatetimeStr, utcStrToDatetimeStr} from "../../formatter";

export function textColumn(
    field:string,
    header:string,
    displayType: DisplayType,
    onClick?: (rowData:any) => void,
) {
    
    var colType = displayType == 'number' 
        ? 'numeric'
        : (displayType == 'datetime' || displayType == 'date' || displayType === 'localDatetime') 
            ? 'date'
            : 'text';
    
    const bodyTemplate = (item: any) => {
        let val = item[field];
        if (val) {
            if (displayType ==="localDatetime") {
                val =  utcStrToDatetimeStr(val) 
            } else if (displayType=== 'datetime') {
                val =  toDatetimeStr(val)
            } else if (displayType==='date') {
                val = toDateStr(val)
            } else if (displayType === 'multiselect') {
                val = val.join(", ")
            }
        }
        return onClick
            ?<div style={{
                cursor: 'pointer',
                color: '#0000EE',
                textDecoration: 'underline'
            }} onClick={()=>onClick(item)}>{val}</div>
            :<>{val}</>
    };
    
    return <Column
        dataType={colType}
        key={field}
        field={field}
        header={header}
        sortable filter body={bodyTemplate}>
    </Column>
}