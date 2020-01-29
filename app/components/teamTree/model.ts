import { SearchableTreeNodeData } from '../common/antd/searchableTree'
export interface DataResponse<T> {
    value: T[];
}
export interface TeamData {
    teamid: string;
    name: string;
    lmr_storeid: { lmr_storeid: string, lmr_name: string };
    lmr_DepartmentId: { lmr_departmentid: string, lmr_name: string };
}
export interface StoreData {
    lmr_storeid: string;
    lmr_name: string;
    lmr_regionid: { lmr_name: string, lmr_regionid: string }
    lmr_number: string | null;
}

export abstract class TeamTreeNode extends SearchableTreeNodeData {
    abstract get selectable(): boolean;
    abstract get iconType(): string;
    abstract get teamId(): string | null;
    abstract get children(): TeamTreeNode[];
}
export class Region extends TeamTreeNode {
    constructor(public key: any, public name: string,public stores: Store[]) {
        super();
    }
    selectable = false;
    get children() {
        return this.stores;
    }
    set children(value: Array<Store>) {
        this.stores = value;
    }
    get teamId() {
        return null;
    }
    iconType = "environment-o"
}
export class Store extends TeamTreeNode {
    constructor(public key: any, public name: string, public teamId: string | null, public departments: Department[], public number: string | null) {
        super();
    }
    get children() {
        return this.departments;
    }
    get selectable() {
        return this.teamId != null;
    }
    iconType = "home"
}
export class Department extends TeamTreeNode {
    selectable: boolean = true;
    constructor(public key: any, public name: string, public teamId: string) {
        super();
    }
    get children() {
        return []
    }
    iconType = "team"
}
export interface ComponentState {
    data: TeamTreeNode[];
    selected?: string;
    searchText: string;
    expandedKeys: string[];
    loading: boolean;
}