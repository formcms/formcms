import {Column} from "primereact/column";
import {AvatarGroup} from "primereact/avatargroup";
import {Avatar} from "primereact/avatar";
import { XAttr } from "../../../cms-client/types/schemaExt";

export function imageColumn({column, getFullAssetsURL}:{
    column: {field:string,header:string}
    getFullAssetsURL ?: (arg:string) =>string | undefined;
}){
    const bodyTemplate = (item:any) => {
        var value = item[column.field];
        const urls:string[] = Array.isArray(value) ? value : [value];
        const fullURLs =  getFullAssetsURL?urls.map(x=>getFullAssetsURL(x )):urls;
        
        return <AvatarGroup>
            {
                fullURLs.map(x=> <Avatar key={x} image={x} size="large" shape="circle" />)
            }
        </AvatarGroup>
    };
    return <Column key={column.field} field={column.field} header={column.header} body={bodyTemplate}></Column>
}