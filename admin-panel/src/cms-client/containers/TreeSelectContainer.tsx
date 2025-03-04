import {TreeSelectInput} from "../../components/inputs/TreeSelectInput";
import {XAttr} from "../types/xEntity";
import { useTree } from "./useTree";

export function TreeSelectContainer(
    {
        data: item, column, id, className, control, register
    }: {
        data: any, column: XAttr, id: any, control: any, register: any, className: string
    }) {

    const targetEntity = column.lookup!
    const options = useTree(targetEntity)
    if (item[column.field] && typeof item[column.field] === "object" ) {
        item[column.field] = item[column.field][targetEntity.primaryKey];
    }


    return <TreeSelectInput
        options={options ?? []}
        data={item}
        column={column}
        control={control}
        className={className}
        register={register}
        id={id}
    />
}