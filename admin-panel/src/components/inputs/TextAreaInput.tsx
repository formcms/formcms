import React from "react";
import {InputTextarea} from "primereact/inputtextarea";
import {InputPanel} from "./InputPanel";

export function TextAreaInput(
    props: {
    data: any,
    column: { field: string, header: string },
    register: any
    className:any
    control:any
        id:any
}) {
    return <InputPanel  {...props} childComponent={ (field:any) =>
        <InputTextarea rows={4} id={field.name} value={field.value} className={' w-full'} onChange={(e) => field.onChange(e.target.value)} />
    }/>
}