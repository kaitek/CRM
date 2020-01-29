import webapi from 'xrm-webapi-client';
import { connectionsFetch, isEmpty } from '../../shared/common';

interface SourceValue {
    key: string,
    contact: string,
    connectionrole: string,
    contactId: string,
    roleId: string
}
const operType = `@odata.bind`;
const braketsTrim = (id: string): string => id.replace("{", "").replace("}", "");

export const connectionCreate = (record: SourceValue): Xrm.Async.PromiseLike<string> => {   
    ////[`record2roleid${operType}`]: `/connectionroles(${braketsTrim(record.roleId)})`,
    let request = {
        [`record1id_account${operType}`]: `/accounts(${braketsTrim(parent.Xrm.Page.data.entity.getId())})`,
        [`record2id_contact${operType}`]: `/contacts(${braketsTrim(record.contactId)})`,
        [`ownerid${operType}`]: `/systemusers(${braketsTrim(parent.Xrm.Page.context.getUserId())})`,
    };
    return Xrm.WebApi.createRecord('connection', request);
}

export const connectionRetrieve = (): any[] | null => {
    const recordId = braketsTrim(parent.Xrm.Page.data.entity.getId());
    let request = {
        entityName: "connection",
        fetchXml: connectionsFetch(recordId),
        async: false
    };
    let res = webapi.Retrieve(request);
    let responseValue: any[] = res.value;
    return responseValue.length > 0 ? responseValue.map(val => {
        return {
            key: val.connectionid,
            contact: val.contact2_x002e_fullname,
            connectionrole: val.connectionrole1_x002e_name,
            contactId: val._record2id_value,
            roleId: val._record2roleid_value
        }
    }) : null;
}
export const connectionUpdateRole = (record: SourceValue) => {
    let request = {
        entityName: "connection",
        entityId: braketsTrim(record.key),
        entity: {
            [`record2roleid${operType}`]: `/connectionroles(${braketsTrim(record.roleId)})`
        }
    };
    webapi.Update(request).then((res: any) => {
        console.log('connectionUpdate', res);
    })
}
export const connectionDelete = (record: SourceValue) => {
    let request = {
        entityName: "connection",
        entityId: braketsTrim(record.key),
        async: true
    };
    webapi.Delete(request);
}