import React from "react";
import {Calendar} from "primereact/calendar";
import {InputPanel} from "./InputPanel";
import {utcStrToDatetime} from "../formatter";

export function LocalDatetimeInput(
    props: {
        data: any,
        column: { field: string, header: string },
        register: any
        className: any
        control: any
        id: any
        inline:boolean
    }) {
    return <InputPanel  {...props} childComponent={(field: any) => {
        const d = field.value && typeof(field.value) === "string"
            ?utcStrToDatetime(field.value)
            : field.value;
       
        
        return <Calendar
            inline={props.inline}
            id={field.name}
            showTime
            hourFormat="24"
            value={d}
            className={'w-full'}
            readOnlyInput={false}
            onChange={
                e => {
                    //json.stringify will convert to utc iso format 2025-03-20T14:55:48.717Z
                    field.onChange(e.value);
                }
            }/>
    }}/>
}