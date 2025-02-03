import React from "react";
import {Calendar} from "primereact/calendar";
import {InputPanel} from "./InputPanel";

export function DatetimeInput(
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
        let d = null
        
        if (field.value) {
            d= new Date(field.value);
        }
        
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
                    if (e.value) {
                        field.onChange(e.value)
                    }
                }
            }/>
    }}/>
}