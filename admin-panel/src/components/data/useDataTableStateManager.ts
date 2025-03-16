import {useReducer} from "react";
import {FilterMatchMode} from "primereact/api";
import { XAttr } from "../xEntity";
import { decodeDataTableState } from "./dataTableStateUtil";

export function useDataTableStateManager(primaryKey :string,rowCount:number, cols: XAttr[], qs?: string|undefined) {
    const defaultState:any = createDefaultState(primaryKey,rowCount,cols,qs);
    const [state, dispatch] = useReducer(reducer, defaultState)
    return {
        state,
        handlers: {
            onPage: (payload: any) => {
                dispatch({type: 'onPage', payload})
            },
            onFilter: (payload: any) => {
                dispatch({type: 'onFilter', payload})
            },
            onSort:(payload :any)=>{
                dispatch({type: 'onSort', payload})
            }
        }
    }
}

function createDefaultState(primaryKey:string,rows:number, cols:XAttr[],qs: string|undefined) {
    
    const defaultState :any= {
        first: 0,
        rows,
        multiSortMeta:[{field:primaryKey,order:-1}],
        filters : createDefaultFilter(cols),
    }
    
    if (qs){
        const s = decodeDataTableState(qs);
        defaultState.first = s.first;
        if (s.rows > 0){
            defaultState.rows = s.rows;
        }
        
        defaultState.multiSortMeta = s.multiSortMeta;
        Object.keys(defaultState.filters).forEach(k =>{
           if (s.filters[k]){
               defaultState.filters[k] = s.filters[k];
           }
        });
    }
    return defaultState
}

function createDefaultFilter(cols:XAttr[]) {
    const getMathMode = (col:XAttr) =>{
        switch (col.displayType){
            case 'number': return FilterMatchMode.EQUALS
            case 'datetime': return FilterMatchMode.DATE_IS
            default: return FilterMatchMode.STARTS_WITH
        }
    }
    const filters:any = {}

    cols.forEach(col =>{
        if (col.displayType == "lookup"){
            filters[col.field + "." + col.lookup!.labelAttributeName] = {operator: 'and', constraints: [{ value: null, matchMode: getMathMode(col) }]}
        }else {
            filters[col.field] = {operator: 'and', constraints: [{ value: null, matchMode: getMathMode(col) }]}
        }
    });
    return filters
}

function reducer(state: any, action: any) {
    const {type, payload} = action
    switch (type) {
        case 'onPage':
            return {...state, first: payload.first, rows: payload.rows}
        case 'onFilter':
            return {...state, filters: {...state?.filters??{},...payload.filters}}
        case 'onSort':
            return {...state, multiSortMeta: payload.multiSortMeta}
    }
    return state
}
