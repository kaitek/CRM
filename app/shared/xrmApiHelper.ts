import WebApiClient from 'xrm-webapi-client';

export interface OptionSet {
    value: string,
    attributeValue: number
}

export function GetOptionSetValues(entityName: string, attributeName: string): OptionSet[] {
    const response = WebApiClient.Retrieve({
        async: false,
        entityName: 'stringmap',
        queryParams: `?$select=attributevalue,value&$filter=objecttypecode eq '${entityName}' and attributename eq '${attributeName}'`
    });
    const optionSets = response.value.map((el: any) => {
        let optionSet: OptionSet = {
            attributeValue: el.attributevalue,
            value: el.value
        };

        return optionSet;
    });

    return optionSets;
}