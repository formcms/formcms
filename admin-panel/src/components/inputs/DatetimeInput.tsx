import React from "react";
import {Calendar} from "primereact/calendar";
import {InputPanel} from "./InputPanel";
import { toDatetime, toZonelessStr } from "../formatter";

export function DatetimeInput(
    props: {
        showTime:boolean,
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
            ? toDatetime(field.value) 
            : field.value;
        
        return <Calendar
            inline={props.inline}
            id={field.name}
            showTime ={props.showTime}
            hourFormat="24"
            value={d}
            className={'w-full'}
            readOnlyInput={false}
            onChange={
                e => {
                    //remove zone info, let backend save date as it is
                    field.onChange(toZonelessStr(e.value));
                }
            }/>
    }}/>
}