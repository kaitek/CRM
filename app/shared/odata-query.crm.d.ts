declare module 'odata-query' {
    export default function(data: QueryData): string
}
interface QueryData {
    top?: number
    select?: string[],
    filter?: Filter | Filter[]
    orderBy?: string[],
    expand?: string | string[] | Expand | Expand[]
}
type Expand = { [key: string]: string | QueryData }
type Filter = string | { [key: string]: { [operator in Operator]?: string | number | null | {type:string,value:string}} | string | number } | { [operator in AndOr]?: Filter[] }
type Operator = 'eq' | 'ne' | 'gt' | 'ge'| 'lt'| 'le';
type AndOr = "and" | "or"