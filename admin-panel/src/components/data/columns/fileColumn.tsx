import {Column} from "primereact/column";

export function fileColumn(
    column: {
        field: any,
        header: any,
    },
    getFullAssetsURL ?: (arg:string) =>string |undefined
) {
    const bodyTemplate = (item: any) => {
        const fullURL =getFullAssetsURL ? getFullAssetsURL(item[column.field]): item[column.field];
        return <a href={ fullURL}>Download</a>;
    };
    return <Column key={column.field} field={column.field} header={column.header} body={bodyTemplate}></Column>
}
