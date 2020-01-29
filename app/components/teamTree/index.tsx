import * as React from 'react'
import webapi from 'xrm-webapi-client'
import Build from 'odata-query'
import { Tree, Icon, Button, Input, Spin, message } from 'antd'
import * as _ from 'lodash'
import { ComponentState, DataResponse, TeamData, StoreData, Region, Department, Store, TeamTreeNode } from './model';
import { CloseThisDialog } from '../../shared/dialogHelper'
import { SearchableTree } from '../common/antd/searchableTree';
import { DialogLayout } from '../common/dialog-layout';

interface TeamTreeState extends ComponentState {
    dataJSON: string;
}

export class TeamTree extends React.Component<any, TeamTreeState> {
    constructor(props: any) {
        super(props)
        this.state = {
            data: [],
            expandedKeys: [],
            searchText: '',
            loading: false,
            dataJSON: ''
        }
    }
    async componentDidMount() {
        this.setState({loading: true})
        try {
            const teams = (await webapi.Retrieve({ entityName: 'team', queryParams: Build({
                select: ['teamid', 'name'],
                filter: { lmr_storeid: {ne: null}},
                expand: [{ lmr_storeid: { select: ['lmr_storeid','lmr_name'] } }]
            })})) as DataResponse<TeamData>;
            const storesWithRegions = (await webapi.Retrieve({entityName: 'lmr_store', queryParams: Build({
                select: ['lmr_storeid', 'lmr_name', 'lmr_number'],
                filter: `Microsoft.Dynamics.CRM.In(PropertyName='lmr_storeid',PropertyValues=[${teams.value.map(t => `'${t.lmr_storeid.lmr_storeid}'`)}])`,
                expand: { lmr_regionid: {select: ['lmr_name', 'lmr_regionid']}}
            })})) as DataResponse<StoreData>;

            const [teamWithFilledDepartments, teamWithEmptyDepartments] = _.partition(teams.value, t => t.lmr_DepartmentId != null);

            const teamsWithNoDepGroupedByStore = _(teamWithEmptyDepartments).keyBy((t: TeamData) => t.lmr_storeid.lmr_storeid).value();
            const teamsWithFilledDepartmentsGroupedByStore = _(teamWithFilledDepartments).groupBy(d => d.lmr_storeid.lmr_storeid).value();
            const teamsGroupedByStoreGroupedByDepartment = _(teamsWithFilledDepartmentsGroupedByStore).mapValues(d => _(d).groupBy(o => o.lmr_DepartmentId.lmr_departmentid).value()).value();
            const storesGroupedByRegion = _(storesWithRegions.value).filter(s => s.lmr_regionid != null).groupBy(s => s.lmr_regionid.lmr_regionid).value();

            const tree = _(storesGroupedByRegion).keys().map((k, ri) => new Region(
                ''+ri,
                storesGroupedByRegion[k][0].lmr_regionid.lmr_name, 
                storesGroupedByRegion[k].map((s, si) => new Store(
                    '' + s.lmr_name+s.lmr_number,
                    s.lmr_name,
                    teamsWithNoDepGroupedByStore[s.lmr_storeid] ? teamsWithNoDepGroupedByStore[s.lmr_storeid].teamid : null,
                    teamsWithFilledDepartmentsGroupedByStore[s.lmr_storeid] && teamsWithFilledDepartmentsGroupedByStore[s.lmr_storeid].map((d, di) => new Department(
                        ''+ri+si+di,
                        d.lmr_DepartmentId.lmr_name,
                        teamsGroupedByStoreGroupedByDepartment[s.lmr_storeid][d.lmr_DepartmentId.lmr_departmentid][0].teamid
                    )),
                    s.lmr_number
                )))).value();
            this.setState({ data: tree, dataJSON: JSON.stringify(tree) })
        } catch (e) {
            message.error('Произошла ошибка при загрузке', 0)
            console.error(e);
        }
        this.setState({loading: false})
    }
    getNodes(data: TeamTreeNode[]): JSX.Element[] {
        if (!data)
            return [];
        return data.map(d => <Tree.TreeNode title={d.name} key={d.key} data={d.teamId} selectable={d.selectable} children={this.getNodes(d.children)} icon={<Icon type={d.iconType}/>}/>)
    }
    render() {
        return <Spin spinning={this.state.loading}>
            <DialogLayout header="Рабочие группы"
                top={<Input.Search className="search-input" onChange={this.onSearchTextChanged.bind(this)}/>}
                bottom={<Button type="primary" onClick={this.assign.bind(this)} disabled={this.state.selected == null} block className="assign-button">Назначить</Button>}>
                <SearchableTree showIcon
                    onSelect={(selectedKeys, { node }) => this.setState({selected: node.props.data})} 
                    className="tree" 
                    data={this.state.data}
                    searchQuery={this.state.searchText}>{this.getNodes(this.state.data)}</SearchableTree>
            </DialogLayout>
        </Spin>
    }
    onSearchTextChanged(e: React.ChangeEvent<HTMLInputElement>) {
        let value = e.target.value!.toLowerCase();
        this.setState({ searchText: value });

        let data: Array<Region> = JSON.parse(this.state.dataJSON);

        data = data.filter(r => {
            if (r.name.toLowerCase().indexOf(value) > -1) {
                r.children = r.stores;
                return true;
            }
            else {
                const isStoreNumber = /^\d+$/.test(value);

                if (isStoreNumber) 
                    r.stores = r.stores.filter(s => s.number!.indexOf(value) > -1);
                else 
                    r.stores = r.stores.filter(s => s.name.toLowerCase().indexOf(value) > -1);

                if (r.stores.length > 0) {
                    r.children = r.stores;

                    return true;
                }

                return false;
            }
        })

        this.setState({ data: data });
    }
    async assign() {
        CloseThisDialog(this.state.selected);
    }
}