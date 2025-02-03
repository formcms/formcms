import { InputPanel } from "./InputPanel"
import { TreeSelect } from 'primereact/treeselect';

export function TreeSelectInput(
    props: {
        data: any,
        options: any[],
        column: { field: string, header: string },
        register: any
        className: any
        control: any
        id: any
    }) {
    const {column,options} = props
    return <InputPanel  {...props} childComponent={(field: any) => {
        return <TreeSelect
            display="chip"
            value={field.value}
            onChange={(e) => {
                console.log("e.value",e.value)
                field.onChange(e.value)
            }}
            options={options}
            placeholder={"Select " + column.header} className="w-full"/>
    }}/>
}