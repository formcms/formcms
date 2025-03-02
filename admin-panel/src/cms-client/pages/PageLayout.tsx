import {useSchema} from "../services/schema";
import React from "react";
import {Helmet} from "react-helmet";
import { Message } from "primereact/message";

interface PageLayoutProps {
    baseRouter:string,
    schemaName:string,
    page: React.FC<{baseRouter:string,schema:any}>;
}

export function PageLayout({baseRouter,schemaName, page:Page}: PageLayoutProps){
    let {data:schema, error} = useSchema(schemaName)
    return <>
        {error && <Message severity={'error'} text={error}/>}
        {schema && <>
            <Helmet>
                <title>🚀{schema?.name} - FormCMS Admin Panel</title>
            </Helmet>
            <Page baseRouter={baseRouter} schema={schema}/>
        </>}
    </>
}