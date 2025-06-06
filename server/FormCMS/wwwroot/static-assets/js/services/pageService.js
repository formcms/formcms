import {getTextWithFullUrl} from "./util.js";

export function getPart(node,source,first,last){
    const url = new URL(window.location.href);
    url.searchParams.append("node",node);
    
    if (source) url.searchParams.append("source",source);
    if (first) url.searchParams.append("first",first);
    if (last) url.searchParams.append("last",last);
    
    return getTextWithFullUrl(url);
}