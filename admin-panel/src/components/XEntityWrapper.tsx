import { Message } from "primereact/message";
import { XEntity } from "./xEntity";

export function XEntityWrapper(
    { 
        baseRouter, 
        useEntityHook, 
        Component
    }: {
        baseRouter: string;
        useEntityHook: () => { data: XEntity|undefined; error: any; isLoading: boolean };
        Component: React.ComponentType<{ baseRouter: string; schema: any }>;
}) {
    const { data: schema, error } = useEntityHook();
    
    return (
        <>
            {error && <Message severity={'error'} text={error}/>}
            {schema && <Component baseRouter={baseRouter} schema={schema} />}
        </>
    );
}