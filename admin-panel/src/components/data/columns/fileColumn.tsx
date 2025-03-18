import {Column} from "primereact/column";

export function fileColumn(
    field:string,
    header:string,
    getFullAssetsURL ?: (arg:string) =>string |undefined
) {
    const bodyTemplate = (item: any) => {
        const fullURL =getFullAssetsURL ? getFullAssetsURL(item[field]): item[field];
        return <a href={ fullURL}>Download</a>;
    };
    return <Column key={field} field={field} header={header} body={bodyTemplate}></Column>
}
