import { FetchingStatus } from "./FetchingStatus";
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
    const { data: schema, error, isLoading } = useEntityHook();
    
    return (
        <>
            <FetchingStatus isLoading={isLoading} error={error} />
            {schema && <Component baseRouter={baseRouter} schema={schema} />}
        </>
    );
}