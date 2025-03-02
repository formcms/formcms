import {useForm} from "react-hook-form";
import {createInput} from "./createInput";
import {XAttr} from "../types/xEntity" ;

export function ItemForm({columns, data, id, onSubmit, formId, uploadUrl, getFullAssetsURL}: {
    columns: XAttr[],
    data: any,
    id?: any
    onSubmit: any
    formId: string
    uploadUrl:string
    getFullAssetsURL : (arg:string) =>string

}) {
    const {
        register,
        handleSubmit,
        control
    } = useForm()

    return columns && <form onSubmit={handleSubmit(onSubmit)} id={formId}>
        <div className="formgrid grid">
            {
                columns.map((column: any) => createInput({data, column, register, control, id, uploadUrl,getFullAssetsURL}))
            }
        </div>
    </form>
}
