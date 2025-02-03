import React from "react";
import {InputText} from "primereact/inputtext";
import {InputPanel} from "./InputPanel";

export function TextInput(
    props: {
        data: any,
        column: { field: string, header: string },
        register: any
        className: any
        control: any
        id: any
    }) {
    return <InputPanel  {...props} childComponent={(field: any) =>
        <InputText
            id={field.name}
            value={field.value ?? ''}
            className={' w-full'}
            onChange={(e) => {
                field.onChange(e.target.value)
            }}/>
    }/>
}