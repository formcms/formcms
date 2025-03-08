
export function ArrayToObject(items: any[]){ 
    return  items.reduce(
        (acc: any, item: any) => {
            if (item.key) acc[item.key] = item.value;
            return acc;
        },
        {}
    );
}