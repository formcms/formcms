
export function getParams(keys){
    const searchParams = new URLSearchParams(window.location.search);
    return keys.map(key => searchParams.get(key));
}