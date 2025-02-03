import React from "react";
import {Calendar} from "primereact/calendar";
import {InputPanel} from "./InputPanel";

export function DateInput(
    props: {
        data: any,
        column: { field: string, header: string },
        register: any
        className:any
        control:any
        id:any
    }) {
    return <InputPanel  {...props} childComponent={ (field:any) =>{
        let d:any = new Date(field.value)
        // @ts-ignore
        if (isNaN(d)){
            d =null
        }
        return <Calendar id={field.name} value={d} className={'w-full'} readOnlyInput={false}  onChange={(e) => {
            if (e.value){
                field.onChange(e.value)
            }
        }} />
    }}/>
}