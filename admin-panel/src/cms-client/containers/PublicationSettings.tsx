import { useForm } from "react-hook-form"
import { DefaultAttributeNames } from "../types/defaultAttributeNames"
import { LocalDatetimeInput } from "../../components/inputs/LocalDatetimeInput";

export function PublicationSettings(
    {formId,data,  onSubmit}:
    {formId:string, data:any,onSubmit:any}
){
    const {
        register,
        handleSubmit,
        control
    } = useForm()
    
    const publishedAt = data[DefaultAttributeNames.PublishedAt] ?? new Date();
    const formData = {[DefaultAttributeNames.PublishedAt]: publishedAt}
    
    return <form  onSubmit={handleSubmit(onSubmit)} id={formId}>
        <LocalDatetimeInput inline className="col-10" data={formData} column={{
            field: DefaultAttributeNames.PublishedAt,
            header: "Published at",
        }} register={register} control={control} id={'publishedAt'}/>
    </form>
}