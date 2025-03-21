import {Column} from "primereact/column";

export function textColumn(
    field:string,
    header:string,
    formatter:any,
    colType: 'numeric'|'date'|'text',
    onClick?: (rowData:any) => void,
) {
    
    const bodyTemplate = (item: any) => {
        var val = item;
        for(const f of field.split('.')){
            if (!val) break;
            val = val[f]
        }
        
        if (val && formatter) {
            val = formatter(val)
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