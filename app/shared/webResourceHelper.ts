export function GetParameter(parameter: string): string | null {
    const regex = new RegExp(`${parameter}=(.+)`);
    const result = decodeURIComponent(location.search)
        .substring(1)
        .split('&')
        .filter(p => p.search(regex) !== -1)
        .map(p => (p.match(regex) as RegExpMatchArray)[1]);
    return result.length ? result[0] : null;
}
export function GetDataParameter(): string | null {
    return GetParameter('data');
}
export function GetLogicalName(): string {
    return parent.Xrm.Page.data.entity.getEntityName();
}
export function GetId(): string | null {
    return parent.Xrm.Page.data.entity.getId().replace(/{|}/g, '').toUpperCase();  
}
export function SubscribeOnFirstSave(ready: (id:string)=>void) {
    if (GetId())
        return;
    parent.Xrm.Page.data.entity.addOnSave(() => {
        waitId(100, ready);
    })
}
function waitId(waitTime: number, end: (id: string)=>void) {
    const id = GetId();
    if (!id)
        setTimeout(waitId.bind(null, waitTime, end), waitTime) 
    else
        end(id)
}
export function GetUserId(): string | null {
    return parent.Xrm.Page.context.getUserId().replace(/{|}/g, '').toUpperCase();
}