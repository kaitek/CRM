import webapi from 'xrm-webapi-client';

export class CrmConfigHelper {
    static GetValue(key: string): string {
        const response: any = webapi.Retrieve({ entityName: 'lmr_config', queryParams: `?$filter=lmr_name eq '${key}'`, async: false });
        let value: string = "";

        if (response)
            if (response.value && response.value.length > 0)
                value = response.value[0].lmr_value;

        return value;
    }

    static async GetValueAsync(key: string): Promise<string> {
        const result = await (<Promise<any>>webapi.Retrieve({ entityName: 'lmr_config', queryParams: `?$filter=lmr_name eq '${key}'` }));

        return result['lmr_value'];
    }
}