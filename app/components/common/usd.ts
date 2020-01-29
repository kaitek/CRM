export function OpenLink(eventName: string, params?: { [key: string]: string}) {
    window.open(`http://event/?eventname=${eventName}&${params ? Object.keys(params).map(k => `${k}=${params[k]}`).join('&') : ''}`);
}