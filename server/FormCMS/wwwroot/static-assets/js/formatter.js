$(document).ready(() => {
    $('[data-component="localDateTime"]').each(function() {
        const isoDate = $(this).text().trim();
        if (isoDate) {
            const formattedDate = utcStrToDatetimeStr(isoDate);
            $(this).text(formattedDate || isoDate);
        }
    });
});

function utcStrToDatetimeStr  (s)  {
    if (!s) return null
    const d = typeof(s) == 'string' ? utcStrToDatetime(s):s;
    return d.toLocaleDateString() + ' ' + d.toLocaleTimeString();
}

const utcStrToDatetime = (s) => {
    s = s.replaceAll(' ', 'T')
    if (!s.endsWith('Z')) {
        s += 'Z';
    }
    return new Date(s);
}