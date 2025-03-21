
export const formatFileSize = (bytes?: number) => {
    if (!bytes || bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

export const toZonelessStr = (date : Date|undefined|null) => {
    if (!date) return null;
    const pad = (num:number) => String(num).padStart(2, '0');
    const formattedDate = `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T` +
        `${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
    return formattedDate;
}

//str can be 2025-03-18 18:50:22.323 And ISO format 2025-03-18T18:50:22.323
export const toDatetime = (s:string) => {
    s = s.replaceAll(' ', 'T');
    var d = new Date(s);
    return d;
}

export const toDateStr = (s:string) => {
    var d = toDatetime(s);
    return d.toLocaleDateString();
}

export const toDatetimeStr = (s:string) => {
    var d = toDatetime(s);
    return d.toLocaleDateString() + ' ' + d.toLocaleTimeString();
}

export const utcStrToDatetime = (s:string) => {
    s = s.replaceAll(' ', 'T')
    if (!s.endsWith('Z')) {
        s += 'Z';
    }

    return new Date(s);
}

export const utcStrToDatetimeStr = (s:string) => {
    var d = utcStrToDatetime(s);
    return d.toLocaleDateString() + ' ' + d.toLocaleTimeString();
}