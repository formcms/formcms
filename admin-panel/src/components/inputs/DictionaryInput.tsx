import React, {useEffect} from "react";
import {Controller, useFieldArray} from "react-hook-form";
import {InputText} from "primereact/inputtext";
import {Button} from "primereact/button";

export function DictionaryInput(
    {
        data,
        column,
        control,
        className,
        id,
    }: {
        data: any;
        column: { field: string; header: string };
        control: any;
        className: any;
        id: any;
    }) {
    const defaultValue = data[column.field] || {}; // e.g., { author: "John", tags: ["photo"] }

    const {fields, append, remove, replace} = useFieldArray({
        control,
        name: column.field, // e.g., "Metadata"
    });

    // Initialize fields from defaultValue if not already set
    useEffect(() => {
        if (Object.keys(defaultValue).length > 0 && fields.length === 0) {
            const initialPairs = Object.entries(defaultValue).map(([key, value]) => ({
                key,
                value,
            }));
            replace(initialPairs); // Replace empty fields with data from defaultValue
        }
    }, [defaultValue, fields.length, replace]);

    return (!id || Object.keys(data).length > 0) ? (
        <div className={className}>
            <label htmlFor={column.field}>{column.header}</label>
            <ul className="list-none p-0 m-0">
                {fields.map((field, index) => (
                    <li
                        key={field.id}
                        className="flex align-items-center py-3 px-2 border-top-1 surface-border flex-wrap"
                    >
                        <div className="text-500 w-6 md:w-2 font-medium">
                            <Controller
                                name={`${column.field}.${index}.key`}
                                control={control}
                                defaultValue={field.id || ""}
                                render={({field: keyField}) => (
                                    <InputText
                                        id={keyField.name}
                                        value={keyField.value ?? ""}
                                        className="w-full"
                                        onChange={(e) => keyField.onChange(e.target.value)}
                                        placeholder="Key"
                                    />
                                )}
                            />
                        </div>
                        <div className="text-900 w-full md:w-8 md:flex-order-0 flex-order-1">
                            <Controller
                                name={`${column.field}.${index}.value`}
                                control={control}
                                defaultValue={field['id'] || ""}
                                render={({field: valueField}) => (
                                    <InputText
                                        id={valueField.name}
                                        value={valueField.value ?? ""}
                                        className="w-full"
                                        onChange={(e) => valueField.onChange(e.target.value)}
                                        placeholder="Value"
                                    />
                                )}
                            />
                        </div>
                        <Button
                            icon="pi pi-trash"
                            className="p-button-danger p-button-sm ml-2"
                            onClick={() => remove(index)}
                        />
                    </li>
                ))}
            </ul>
            <Button
                type="button"
                label="Add Key-Value Pair"
                icon="pi pi-plus"
                className="p-button-secondary mt-2"
                onClick={() => append({key: "", value: ""})}
            />
        </div>
    ) : null;
}