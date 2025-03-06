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

        path,
        setPath,
        paths,
        setPaths,
    }:
    {
        state: any
        onPage: any,
        data: ListResponse 
        
        getAssetUrl: (s: string) => string
        pathField:string
        nameField:string
        titleField:string
        
        path?:string,
        setPath?:(path:string) => void,

        paths?:string[]|undefined,
        setPaths?:(paths:string[]) => void,
    }) {
    const gridItem = (asset: any) => {

        // Handle multiple selection
        const isMultiSelectMode = setPaths && paths !== undefined;
        const isChecked = paths?.includes(asset[pathField]) || false;

        const toggleMultiSelect = isMultiSelectMode ? () => {
            if (isChecked) {
                setPaths((paths || []).filter(p => p !== asset[pathField]));
            } else {
                setPaths([...(paths || []), asset[pathField]]);
            }
        } : undefined;

        // Handle single selection
        const handleSingleSelect = setPath && path !== undefined
            ? () => setPath(asset[pathField])
            : undefined;

        // Visual indication for selected items
        const isSelected = path === asset[pathField] || isChecked;
        const selectedClass = isSelected ? 'surface-hover' : '';

        // Checkbox for multiple selection
        const checkbox = isMultiSelectMode ? (
            <input
                type="checkbox"
                checked={isChecked}
                onChange={toggleMultiSelect}
            />
        ) : null;


        return (
            <div className="col-12 sm:col-6 lg:col-12 xl:col-3 p-2" key={asset[pathField]}>
                <div
                    className={`p-4 border-1 surface-border surface-card border-round ${selectedClass}`}
                    onClick={handleSingleSelect || toggleMultiSelect}
                    style={handleSingleSelect || toggleMultiSelect ? {cursor: 'pointer'} : {}}
                >
                    <div className="flex flex-column align-items-center gap-3 py-5">
                        {checkbox ? (
                            <div className="flex align-items-center gap-2">
                                {checkbox}
                            </div>
                        ) : null}
                        <img className="w-9 shadow-2 border-round" src={`${getAssetUrl(asset[pathField])}`}/>
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
