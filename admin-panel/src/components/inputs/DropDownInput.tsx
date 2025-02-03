import {Dropdown} from "primereact/dropdown";
import {InputPanel} from "./InputPanel";
import React from "react";

export function DropDownInput(props: {
    data: any,
    column: { field: string, header: string},
    options:string[],
    control: any
    className: any
    register: any
    id:any
}) {
    return <InputPanel  {...props} childComponent={ (field:any) =>
        <Dropdown
            id={field.name}
            value={field.value}
            options={props.options}
            focusInputRef={field.ref}
            onChange={(e) => field.onChange(e.value)}
            className={'w-full'}
        />
    }/>
}