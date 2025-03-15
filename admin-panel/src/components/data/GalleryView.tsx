import { ListResponse } from "./listResponse"
import {DataView} from 'primereact/dataview';

export function GalleryView(
    {
        state,
        onPage,
        data,
        
        getAssetUrl,
        pathField,
        nameField,
        titleField,
        typeField,

        path,
        onSelect,
    }:
    {
        state: any
        onPage: any,
        data: ListResponse 
        
        getAssetUrl: (s: string) => string
        pathField:string
        nameField:string
        titleField:string
        typeField:string
        
        path?:string,
        onSelect:(asset:any) => void,
    }) {
    const gridItem = (asset: any) => {
        const defaultImage = 'https://placehold.co/600x400?text=' + asset[nameField];
        const src = asset[typeField].startsWith('image') ? getAssetUrl(asset[pathField]): defaultImage;
        
        const selectedClass = path === asset[pathField] ? 'surface-hover' : '';
        return (
            <div className="col-12 sm:col-6 lg:col-12 xl:col-3 p-2" key={asset[pathField]}>
                <div
                    className={`p-4 border-1 surface-border surface-card border-round ${selectedClass}`}
                    onClick={()=>onSelect(asset)}
                    style={{cursor: 'pointer'}} 
                >
                    <div className="flex flex-column align-items-center gap-3 py-5">
                        <img className="w-9 shadow-2 border-round" src={src}/>
                        <div className="text-1xl">{asset[nameField]}</div>
                        <div className="text-1xl">{asset[titleField]}</div>
                    </div>
                </div>
            </div>
        );
    };

    return <div className="card">
        <DataView value={data.items}
                  rows={state.rows}
                  first={state.first}
                  totalRecords={data.totalRecords}
                  itemTemplate={gridItem}
                  onPage={onPage}
                  lazy
                  paginator/>
    </div>
}
