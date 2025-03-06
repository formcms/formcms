import qs from 'qs';

export function decodeDataTableState(querystring :string){
    const s = qs.parse(querystring);
    const state :any = {
        first: s.offset,
        rows: s.limit,
        multiSortMeta: convertSortMapToArray(s.sort as any)
    }
    state.filters = {};
    Object.entries(s).forEach(([k,v])=>{
        if (k != "offset" && k != "limit" && k != "sort" ){
            state.filters[k] = unFlatFilter(v as any);
        }
    });
    return state;
}

export function encodeDataTableState(state:any) {
    if (!state){
        return ''
    }
    state = deepClone(state);
    return qs.stringify(
        sanitizeState(state),
        {encodeValuesOnly: true, skipNulls: true, arrayFormat:'repeat'}
    );
}

function sanitizeState(payload:any){
    sanitizeFilter(payload.filters);
    flatFilter(payload.filters);

    const state = {
        offset: payload.first,
        limit: payload.rows,
        ...payload.filters,
        sort: convertSortArrayToMap(payload.multiSortMeta),
    }
    deleteEmptyProperties(state)
    return state
}

function convertSortMapToArray(sorts:any){
     return Object.entries(sorts ??[]).map(([field, order])=>({field,order}))
}

function convertSortArrayToMap(sorts:any[]){
    const item:any = {};
    (sorts??[]).forEach((sort:any) =>{
        item[sort.field] = sort.order;
    })
    return item
}
function unFlatFilter(condition:any) {
    const ret:any = {constraints:[]};
    Object.entries(condition).forEach(([matchMode,val])=>{
        if (matchMode != "operator"){
            if (Array.isArray(val)){
                val.forEach(v =>{
                    ret.constraints.push({matchMode, value: v});
                })
            }else {
                ret.constraints.push({matchMode, value: val});
            }
        }else {
            ret[matchMode] = val;
        }
    })
    return ret;
}

function flatFilter(filter:{[k: string]: any}){
    Object.entries(filter??{}).forEach( ([_,item])=>{
        item.constraints.forEach((constraint:any) =>{
            if (!item[constraint.matchMode]){
                item[constraint.matchMode] = [constraint.value];
            }else {
                item[constraint.matchMode].push(constraint.value);
            }
        })
        delete item.constraints;
    })
    return filter
}

function sanitizeFilter(filter: any) {
    Object.keys(filter ?? {}).forEach(k => {
        const item = filter[k]
        if (item?.constraints?.length > 0 && item.constraints[0].value === null) {
            delete filter[k]
        }else if (item?.constraints?.length == 1 || item?.operator == 'and'){
            delete filter[k].operator
        }
    })
}

function deleteEmptyProperties(obj: any) {
    for (const prop in obj) {
        if (obj[prop] === null || obj[prop] === undefined) {
            delete obj[prop];
        }
    }
}

function deepClone(obj: any): any {
    if (obj === null || typeof obj !== 'object') {
        return obj;
    }

    if (Array.isArray(obj)) {
        const arrCopy = [] as any[];
        for (const item of obj) {
            arrCopy.push(deepClone(item));
        }
        return arrCopy as any;
    }

    const objCopy = {} as { [key: string]: any };
    for (const key in obj) {
        if (obj.hasOwnProperty(key)) {
            objCopy[key] = deepClone(obj[key]);
        }
    }
    return objCopy ;
}