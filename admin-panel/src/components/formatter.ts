
export const formatFileSize = (bytes?: number) => {
    if (!bytes || bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

//consider both 2025-03-18 18:50:22.323 And 2025-03-18 18:50:22.323 as UTC time.
export const formatDate = (s:string) => {
    s = s.replaceAll(' ', 'T')+"+00:00";
    var d = new Date(s);
    return d.toLocaleDateString() + ' ' + d.toLocaleTimeString();
}