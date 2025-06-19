
export function utcStrToDatetimeStr  (s)  {
    if (!s) return null
    const d = typeof(s) == 'string' ? utcStrToDatetime(s):s;
    return d.toLocaleDateString() + ' ' + d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

function utcStrToDatetime  (s)  {
    s = s.replaceAll(' ', 'T')
    if (!s.endsWith('Z')) {
        s += 'Z';
    }
    return new Date(s);
}

export function formatCount(count) {
    if (count >= 1000) {
        const val = (count / 1000).toFixed(1);
        return val.endsWith('.0') ? `${val.slice(0, -2)}k` : `${val}k`;
    }
    return count.toString();
}