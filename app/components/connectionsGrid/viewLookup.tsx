import { b2bContactfetchXML, connectionRoleFetchXML } from '../../shared/common';
import { XMLLayouts } from '../../crm/fetchXML';
import * as Randexp from 'randexp';
import { isEmpty } from '../../shared/common'


interface LookupFiltering extends Xrm.LookupOptions {
    customViews?: any[],
    disableViewPicker?: boolean,
    searchText?: string,
    allowFilterOff?: boolean,
    filterRelationshipDependantAttribute?: string,
    filterRelationshipId?: string,
    filterRelationshipName?: string,
    filterRelationshipType?: string
}
interface CView {
    name: string,
    fetchXml: string,
    layoutXml: string,
    id: string,
    recordType: number,
    Type: number
}
interface LookupItem {
    id: string,
    typename: string,
    name: string
}
interface CViewArgv {
    viewName: string,
    fetch: string,
    layoutXml: string,
    id: string
}

const customView = (props: CViewArgv): CView => {
    const { viewName, fetch, layoutXml, id } = props;
    return {
        id: id,
        recordType: 2,
        name: viewName,
        fetchXml: fetch,
        layoutXml: layoutXml,
        Type: 0
    }
}
const lookupOptions = (value: string, entityName: string): LookupFiltering => {
    let id = new Randexp(/^{[0-9]{8}-[0-9]{4}-[0-9]{4}-[0-9]{4}-[0-9]{12}}$/).gen();
    let layoutXml = "";
    let viewName = "";
    let fetch = "";

    switch (entityName) {
        case 'contact':
            layoutXml = XMLLayouts.ContactsOnPowerOfAttorney;
            viewName = "B2b Контакты";
            fetch = b2bContactfetchXML(entityName);
            break;
        case 'connectionrole':
            layoutXml = XMLLayouts.Connectionrole;
            viewName = "Роли подключения";
            fetch = connectionRoleFetchXML(entityName);
            break;
    }
    return {
        allowMultiSelect: false,
        defaultEntityType: entityName,
        entityTypes: [entityName],
        customViews: [customView({ viewName, fetch, layoutXml, id })],
        viewIds: [],
        defaultViewId: id,
        searchText: value,
        disableViewPicker: true,

        //allowFilterOff: true,
        //filterRelationshipDependantAttribute: "connectionroleobjecttypecode.associatedobjecttypecode",
        //filterRelationshipType: '1',
        //filterRelationshipName: "connectionroleobjecttypecode"
    };
}

export const viewLookup = (value: string, dataIndex: string, form?: any, save?: Function, e?: any, handleSave?: Function) => {
    parent.Xrm.Utility.lookupObjects(lookupOptions(value, dataIndex))
        .then((result: Xrm.LookupValue | LookupItem[]) => { //Xrm.LookupValue invalid microsoft typescript interface
            let res = result as LookupItem[];
            if (res.length > 0) {
                if (isEmpty(form)) {
                    handleSave!(
                        {
                            connectionrole: "", contact: res[0].name,
                            contactId: res[0].id, key: "", roleId: ""
                        });
                } else {
                    form.setFieldsValue({
                        [dataIndex]: res[0].name,
                    });
                    save!(form, { id: res[0].id, name: res[0].name }, e);
                }
            }
        },
            (error) => console.error(error)
        );
}