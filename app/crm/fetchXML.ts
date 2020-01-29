
export interface Condition {
    operator: string,
    attribute: string,
    uitype?: string,
    value?: string
}

export interface Order {
    attribute?: string,
    descending?: boolean
}

export class Filter {
    type: string;
    conditions: Condition[];
    filters?: Filter[];

    constructor(type: string, conditions: Condition[], filters: Filter[]) {
        this.type = type;
        this.conditions = conditions;
        this.filters = filters;
    }
}

export class Entity {
    public readonly entityName: string;
    public readonly attributes: string[];
    public readonly filter?: Filter;
    public readonly linkEntity?: LinkEntity;
    public readonly distinct?: boolean;

    public constructor(entityName: string, attributes: string[], filter: Filter, linkEntity?: LinkEntity, distinct?: boolean) {
        this.entityName = entityName;
        this.attributes = attributes;
        this.filter = filter;
        this.linkEntity = linkEntity;
        this.distinct = distinct;
    }
}

export class LinkEntity extends Entity {
    public readonly from: string;
    public readonly to: string;
    public readonly alias: string;

    public constructor(entityName: string, attributes: string[], from: string, to: string, alias: string, filter: Filter, linkEntity?: LinkEntity) {
        super(entityName, attributes, filter, linkEntity);

        this.from = from;
        this.to = to;
        this.alias = alias;
    }
}

export class FetchXML {
    public static FetchXMLCreator(entity: Entity, order?: Order): string {
        var orderXML: string = order ? `<order attribute="${order.attribute}" descending="${order.descending}" />` : "";
        var innerXML: string = `<entity name="${entity.entityName}">
                                    ${FetchXML.GetFetchXMLAttributes(entity)}
                                    ${orderXML}
                                    ${FetchXML.GetFetchXMLLinkEntityFilter(entity.linkEntity)}
                                    ${FetchXML.GetFetchXMLFilter(entity.filter)}
                                </entity>`;
        var fetchXML: string = `<fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="${!entity.distinct ? false : true}">
                                    ${innerXML}
                                </fetch>`

        return fetchXML;
    }

    public static GetFetchXMLFilter(filter?: Filter): string {
        var mainFilterXML = "";
        var innerFiltersXML = "";

        if (filter) {
            if (filter.filters)
                var innerFiltersXML = filter.filters
                    .map((filter) => `<filter type="${filter.type}">${FetchXML.GetFetchXMLConditions(filter.conditions)}</filter>`)
                    .join("");

            mainFilterXML = `<filter type="${filter.type}">
                                ${FetchXML.GetFetchXMLConditions(filter.conditions)}
                                ${innerFiltersXML}
                             </filter>`;
        }

        return mainFilterXML;
    }

    private static GetFetchXMLConditions(conditions: Condition[]): string {
        return conditions.map((attr: Condition) => {
            return `<condition attribute="${attr.attribute}" uitype="${attr.uitype}" operator="${attr.operator}" value="${attr.value}"/>`
        })
            .join("");
    }

    private static GetFetchXMLAttributes(entity: Entity): string {
        const linkEntity = entity.linkEntity;

        var mainAttributesXML: string = entity.attributes
            .map((attr: string) => { return `<attribute name="${attr}"/>` })
            .join("");
        var linkEntityAttributesXML: string = "";

        if (linkEntity && linkEntity.attributes.length > 0) {
            linkEntityAttributesXML = `<link-entity name="${linkEntity.entityName}" from="${linkEntity.from}" to="${linkEntity.to}" alias="${linkEntity.alias}">
                                            ${linkEntity.attributes.map((attr: string) => { return `<attribute name="${attr}"/>` }).join("")}
                                       </link-entity>`;
        }

        return mainAttributesXML + "\n" + linkEntityAttributesXML;
    }

    private static GetFetchXMLLinkEntityFilter(linkEntity?: LinkEntity): string {
        var linkEntityFilterXML = "";

        if (linkEntity) {
            linkEntityFilterXML = `<link-entity name="${linkEntity.entityName}" from="${linkEntity.from}" to="${linkEntity.to}" alias="${linkEntity.alias}">
                                        ${FetchXML.GetFetchXMLFilter(linkEntity.filter)}
                                        ${this.GetFetchXMLLinkEntityFilter(linkEntity.linkEntity)}
                                   </link-entity>`;
        }

        return linkEntityFilterXML;
    }
}

export class XMLLayouts {
    public static ContactsOnPowerOfAttorney: string =
        `<grid name='contactsOnPowerOfAttorney' object='8' jump='fullname' select='1' preview='1' icon='1'>
                <row name='result' id='contactid'>
                     <cell name='fullname' width='300' />
                     <cell name='lmr_customertype' width='100' />
                     <cell name='birthdate' width='100' />
                     <cell name='mobilephone' width='150' />
                     <cell name='emailaddress1' width='200' />
                     <cell name='lmr_loyaltycardnumber' width='150' />
                </row>
             </grid>`;
    public static Connectionrole: string =
        `<grid name='Connectionrole' object='4' jump='name' select='1' preview='1' icon='1'>
                <row name='result' id='connectionroleid'>
                     <cell name='name' width='250' />
                     <cell name='category' width='300' />
                </row>
             </grid>`;
}