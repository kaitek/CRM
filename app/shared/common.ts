import webapi from 'xrm-webapi-client'
import { FetchXML, XMLLayouts, Condition } from '../crm/fetchXML';

declare global {
    interface String {
        format: (...args: any[]) => string;
    }
}
export function RetrieveUserStore(userId: string, callback: (store: any) => void, onerror: (error: any) => void): void {
    webapi.Retrieve({
        entityName: "systemuser",
        entityId: userId,
        queryParams: '?$select=_lmr_storeid_value&$expand=lmr_StoreId($select=lmr_name)'
    }).then((result: any) => {
        if (result['_lmr_storeid_value']) {
            var storeName: string = result["lmr_StoreId"]["lmr_name"];
            callback({ entityType: 'lmr_store', id: result['_lmr_storeid_value'], name: storeName });
        }
    })
        .catch(onerror);
}
export function isEmpty(obj: any): boolean {
    return (typeof obj === "undefined" || obj === null || obj === "");
}
export function setReadonly(flag: boolean, ...args: any[]): void {
    args.length > 0 ? (flag ? args.forEach(arg => !isEmpty(arg) ? arg.setDisabled(false) : null) :
        args.forEach(arg => !isEmpty(arg) ? arg.setDisabled(true) : null)) : null;
}
export function toTrimBrackets (val:string): string {
    return !isEmpty(val) ? val.replace('{', '').replace('}', '').toLowerCase() : "";
} 
export function SetB2BFilteringViewOnContactLookup(formContext: Xrm.FormContext, xmlLayout: string, attrName: string): void {
    const contactControl = formContext.getControl<Xrm.Controls.LookupControl>(attrName);

    const entityName = "contact";
    const displayName = "B2B контакты";

    contactControl.addCustomView(contactControl.getDefaultView(), entityName,
        displayName, b2bContactfetchXML(entityName),
        xmlLayout,
        true
    );
}

export function SetSelectedAccountFilteringViewOnContactLookup(formContext: Xrm.FormContext): void {
    const contactControl = formContext.getControl<Xrm.Controls.LookupControl>("lmr_contactid");
    const account: Xrm.Attributes.LookupAttribute = formContext.getAttribute("lmr_accountid");
    const accountValue = account.getValue() == null ? "00000000-0000-0000-0000-000000000000" : account.getValue()[0].id;
    const viewId = "ba0e3fcb-e1f1-4d94-a414-fd1fd909a675";

    var connections = webapi.Retrieve({
        entityName: "connection",
        fetchXml: connectionsFetchXML(accountValue),
        async: false
    });

    var contactsConditions: Condition[];

    if (connections.value.length == 0)
        contactsConditions = [{ attribute: "contactid", uitype: "contact", operator: "null" }];
    else
        contactsConditions = connections.value.map((con: any) => {
            return { attribute: "contactid", uitype: "contact", operator: "eq", value: con._record2id_value }
        });

    var contactsFetchXML: string = FetchXML.FetchXMLCreator(
        {
            entityName: "contact",
            attributes: ["fullname", "lmr_customertype", "birthdate", "mobilephone", "emailaddress1", "lmr_loyaltycardnumber"],
            filter: {
                filters: [{ type: "or", conditions: contactsConditions }],
                conditions: [
                    { attribute: "statecode", operator: "eq", value: "0" },
                    { attribute: "lmr_customertype", operator: "eq", value: "176670001" },
                ],
                type: "and"
            }
        }
    );

    contactControl.addCustomView(viewId,
        "contact", "Контакты выбранной организации",
        contactsFetchXML, XMLLayouts.ContactsOnPowerOfAttorney,
        false
    );
}
export const b2bContactfetchXML = (entityName: string): string => {
    return FetchXML.FetchXMLCreator(
        {
            entityName: entityName,
            attributes: ["fullname", "lmr_customertype", "birthdate", "mobilephone", "emailaddress1", "lmr_loyaltycardnumber"],
            filter: {
                conditions: [
                    { attribute: "lmr_customertype", operator: "eq", value: "176670001" },
                    { attribute: "statecode", operator: "eq", value: "0" },
                ],
                type: "and"
            }
        },
        {
            attribute: "fullname",
            descending: false
        }
    );
}
export const connectionRoleFetchXML = (entityName: string): string => {
    return FetchXML.FetchXMLCreator(
        {
            entityName: entityName,
            distinct: true,
            attributes: ["name", "category", "connectionroleid"],
            linkEntity: {
                entityName: 'connection', from: 'record1roleid',
                to: 'connectionroleid', alias: '', attributes: [],
                filter: {
                    conditions: [
                        { attribute: 'record1objecttypecode', operator: 'eq', value: '2' }
                    ],
                    type: 'and'
                }
            },
        },
    );
}

export const connectionsFetchXML = (accountValue: string): string => {
    return FetchXML.FetchXMLCreator(
        {
            entityName: "connection",
            attributes: ["record2id"],
            filter: {
                conditions: [
                    { attribute: "record1id", operator: "eq", uitype: "account", value: accountValue },
                    { attribute: "statecode", operator: "eq", value: "0" }
                ],
                type: "and"
            }
        }
    );
}
export const connectionsFetch = (accountId: string): string => {
    return `<fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false" no-lock="true">
              <entity name="connection" >
                <attribute name="record2id" />
                <attribute name="record2roleid" />
                <filter type="and" >
                  <condition attribute="record1id" uitype="account" operator="eq" value="${accountId}" />
                  <condition attribute="statecode" uitype="undefined" operator="eq" value="0" />
                </filter>
                <link-entity name="connectionrole" from="connectionroleid" to="record2roleid" link-type="outer"  >
                  <attribute name="name" />
                </link-entity>
                <link-entity name="contact" from="contactid" to="record2id" >
                  <attribute name="fullname" />
                </link-entity>
              </entity>
            </fetch>`;
}   
    String.prototype.format = function (...args) {
        return this.replace(/\{(\d+)\}/g, (m, n) => {
            return args[0][n] ? args[0][n] : m
        });
};

export function addPreSearch(control: Xrm.Controls.LookupControl, handler: any) {
    control.addPreSearch(handler);
}