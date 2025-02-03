import {InputPanel} from "./InputPanel";
import React from "react";
import {MultiSelect} from "primereact/multiselect";

export function MultiSelectInput(
    props: {
        data: any,
        column: { field: string, header: string},
        options: string[],
        register: any
        className: any
        control: any
        id: any
    }) {
    const {column,options} = props
    console.log(column,options)
    return <InputPanel  {...props} childComponent={(field: any) => {
        return <MultiSelect
            display="chip"
            value={field.value}
            onChange={(e) => {
                return field.onChange(e.value)
            }}
            options={options}
            placeholder={"Select " + column.header} className="w-full"/>
    }}/>
}
