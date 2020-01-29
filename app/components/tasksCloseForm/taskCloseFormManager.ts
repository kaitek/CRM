import webapi from 'xrm-webapi-client';

export interface OpenedTask {
    hoveringMouseOnLink: boolean,
    scheduledend: string,
    solution: string,
    subject: string,
    key: string
}

export class TaskCloseFormManager {
    public static async GetOpenedTasksAsync(opportunityId: string | null): Promise<OpenedTask[]> {
        if (!opportunityId)
            throw 'Не найден идентификатор продажи';

        const query = `?$select=new_solution,scheduledend,subject&$filter=_regardingobjectid_value eq ${opportunityId} and statecode eq ${0}`;
        const response: any = await webapi.Retrieve({ async: true, entityName: 'task', queryParams: query });

        return response.value.map((element: any): OpenedTask => {
            const scheduledendDate: Date = new Date(element.scheduledend);
            const scheduledendStr: string = element.scheduledend ?
                `${scheduledendDate.toLocaleDateString()} ${scheduledendDate.toLocaleTimeString()}` : "";

            return {
                hoveringMouseOnLink: false,
                solution: element.new_solution,
                scheduledend: scheduledendStr,
                subject: element.subject,
                key: element.activityid
            };
        });
    }

    public static async CloseTaskAsync(activityId: string, solution: string, statecode: number): Promise<any> {
        await webapi.Update({
            entityId: activityId,
            entityName: 'task',
            entity: {
                'new_solution': solution,
                'statecode': statecode
            },
            async: true
        });
    }
}