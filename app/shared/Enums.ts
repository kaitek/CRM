export const enum StatusCode {
    lost = 5,
    canceled = 6,
    recalculation = 7
}

export const enum ContractStatusCode {
    signed = 176670000,
    unsigned = 176670001,
}

export const enum ReasonOfFail {
    expensive = 176670000,
    noSizeOrStyles = 176670001,
    noProduct = 176670002,
    other = 176670003,
    recalculation = 176670004
}

export const enum TypeOfSaleCode {
    Unknown = 0,
    physical = 176670000,
    organization = 176670001,
}

export const enum CustomerTypes {
    Unknown = 0,
    Individual = 176670000,
    Legal = 176670001
}
export const enum AccountType {
    Legal = 176670000,
    IP = 176670001
}

export const enum TaskStateCode {
    open = 0,
    finished = 1,
    canceled = 2,
    planned = 3
}

export const enum TaskStatusCode {
    notstarted = 2,
    executed = 3,
    waiting = 4,
    finished = 5,
    canceled = 6,
    delayed = 7,
}

export const TaskStateOpen: string = "Открыть";
export const TaskStateFinished: string = "Завершено";
export const TaskStateCanceled: string = "Отменено";
export const TaskStatePlanned: string = "Запланировано";

export const TaskStatusNotStarted: string = "Не начато";
export const TaskStatusExecuted: string = "Выполняется";
export const TaskStatusWaiting: string = "Ожидание кого-либо";
export const TaskStatusFinished: string = "Завершено";
export const TaskStatusCanceled: string = "Отменено";
export const TaskStatusDelayed: string = "Отложено";

export const enum AddressTypeCode {
    Invoicing = 176670000,
    Legal = 176670001,
    Actual = 176670002,
}

export const enum OpportunityStateCode {
    Open = 0,
    Done = 1,
    Lost = 2
}
export const enum CaseOriginCode {
    merchant = 176670005,   
}

export const enum IncidentStatusCode { 
    New = 176670000,
    Store = 176670001,
    InWork = 1,
}

export const enum LeadStatusCode {
    Qualified = 3,
}

export const enum LeadStateCode {
    Qualified = 1,
}

export const enum OpportunityStatusCode {
    InProcess = 1,
    Delay = 2,
    Won = 3,
    Canceled = 4,
    NoGood = 5,   
    Lead = 176670000,
    Measure = 176670001,
    Contract = 176670002,
    Payment = 176670003,
    NoCredit = 176670004,
    Expensive = 176670005,
    NoSize = 176670006,
    Other = 176670007,   
    Delivery = 176670008,
    Installation = 176670009,
    BuyInHypermarkt = 176670010,
    BuyCompetitor = 176670011,
} 