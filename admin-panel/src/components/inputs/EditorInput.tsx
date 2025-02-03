import {Editor} from 'primereact/editor';
import {InputPanel} from "./InputPanel";
import React from "react";
const renderHeader = () => {
    return <>
            <span className="ql-formats">
                 <select className="ql-header" defaultValue="5">
                    <option value="3">Heading</option>
                    <option value="4">Subheading</option>
                    <option value="5">Normal</option>
                </select>
            </span>
        <select className="ql-size" defaultValue="medium">
            <option value="small">Small</option>
            <option value="medium">Medium</option>
            <option value="large">Large</option>
        </select>

        <span className="ql-formats">
                    <button className="ql-bold" aria-label="Bold"></button>
                    <button className="ql-italic" aria-label="Italic"></button>
                    <button className="ql-underline" aria-label="Underline"></button>
                </span>
        <span className="ql-formats">
                <button className="ql-list" value="ordered"/>
                <button className="ql-list" value="bullet"/>
                <button className="ql-indent" value="-1"/>
                <button className="ql-indent" value="+1"/>
            </span>
        <span className="ql-formats">
                <select className="ql-align"/>
                <select className="ql-color"/>
                <select className="ql-background"/>
            </span>
        <span className="ql-formats">
                <button className="ql-link"/>
                <button className="ql-image"/>
                <button className="ql-video"/>
            </span>
    </>
};
export function EditorInput(props: {
    data: any
    control: any
    column: { field: string, header: string },
    className: any
    register: any
    id: any

}) {
    return <InputPanel  {...props} childComponent={ (field:any) =>
        <Editor id={field.name} name={props.column.field} value={field.value}
                headerTemplate={renderHeader()}
                onTextChange={(e) => field.onChange(e.htmlValue)}
                style={{height: '320px'}}/>
        }/>
}