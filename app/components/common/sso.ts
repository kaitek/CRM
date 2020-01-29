export interface SsoService {
    code: string,
    friendlyName: string
}
export const SsoServices: SsoService[] = 
    [{ code: 'ldap', friendlyName: 'LDAP'},
    { code: 'pyxis', friendlyName: 'Pyxis'},
    { code: 'nextcontact', friendlyName: 'Next contact'},
    { code: 'puz', friendlyName: 'PUZ' },
    { code: 'ucc', friendlyName: 'UCC' }]