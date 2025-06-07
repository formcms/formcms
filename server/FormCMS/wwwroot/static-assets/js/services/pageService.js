
export async function getPart(node,source,first,last){
    const url = new URL(window.location.href);
    url.searchParams.append("node",node);
    
    if (source) url.searchParams.append("source",source);
    if (first) url.searchParams.append("first",first);
    if (last) url.searchParams.append("last",last);
    
    const res= await fetch(url);
    if (!res.ok) {
        return {error: res.text()}
    }
    return await res.text();
    
}