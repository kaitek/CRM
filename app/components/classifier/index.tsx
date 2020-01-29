import * as React from 'react'
import { Input, Tree, Button } from 'antd'
import * as _ from 'lodash'
import webapi from 'xrm-webapi-client'
import Build from 'odata-query'
import { SearchableTree, SearchableTreeNodeData } from '../common/antd/searchableTree';
import { DialogLayout } from '../common/dialog-layout';
import { CloseThisDialog } from '../../shared/dialogHelper';

interface ClassifierEntity {
    lmr_classifierid: string,
    lmr_name: string,
    _lmr_parentclassifierid_value: string;
    "lmr_storetypecode@OData.Community.Display.V1.FormattedValue": string;
}
class ClassifierLevel extends SearchableTreeNodeData {
    constructor(public key: string, public name: string, public children: ClassifierLevel[]) {
        super();
    }
}
class ClassifierReference {
    constructor(public id: string, public name: string, public parentId?: string) {}
}
interface ComponentState {
    initialData: ClassifierEntity[],
    data: ClassifierLevel[];
    parentMap: { [id: string]: ClassifierReference },
    searchText?: string;
    selectedPath?: ClassifierReference[];
}
export class Classifier extends React.Component<any, ComponentState> {
    constructor(props: any) {
        super(props)
        this.state = {
            data: [],
            initialData: [],
            parentMap: {}
        }
    }
    async componentDidMount() {
        const classifiers = (await webapi.Retrieve({
            headers: [{ key: 'Prefer', value: 'odata.include-annotations=OData.Community.Display.V1.FormattedValue'}],
            entityName: 'lmr_classifier', 
            queryParams: Build({
                select: ['lmr_classifierid', 'lmr_name', '_lmr_parentclassifierid_value', 'lmr_storetypecode'],
                filter: { "statecode": 0 }
            })})).value as ClassifierEntity[];
        const data = this.buildTree(classifiers, null);
        this.setState({
            parentMap: _(classifiers).keyBy(c => c.lmr_classifierid).mapValues(c => new ClassifierReference(c.lmr_classifierid, c.lmr_name, c._lmr_parentclassifierid_value)).value(),
            data: data,
            initialData: classifiers
        })
    }
    buildTree(data: ClassifierEntity[], parentid: string | null, condition?: (l: ClassifierLevel)=>boolean): ClassifierLevel[] { 
        const dataPartition = _.partition(data, (cd: ClassifierEntity) => cd._lmr_parentclassifierid_value == parentid)
        let tree = dataPartition[0].map(c => new ClassifierLevel(c.lmr_classifierid, c.lmr_name, this.buildTree(dataPartition[1], c.lmr_classifierid, condition)))
        if (condition)
            tree = tree.filter(condition)
        return tree;
    }
    getTreeNodes(data: ClassifierLevel[]):any {
        return data.map(l => <Tree.TreeNode title={l.name} key={l.key} data={l} children={this.getTreeNodes(l.children)}/>)
    }

    render() {
        return <DialogLayout header="Классификатор"
            top={<Input.Search onSearch={value => this.setState({searchText: value})} enterButton />}
            bottom={<Button type="primary" onClick={() => CloseThisDialog(this.state.selectedPath)} disabled={!this.state.selectedPath} block>Выбрать</Button>}>
            <SearchableTree data={this.state.data} searchQuery={this.state.searchText} onSearch={this.onSearch.bind(this)} onSelect={(_, { selected,  node }) => selected ? this.onSelect(node) : this.setState({ selectedPath: undefined})}>{this.getTreeNodes(this.state.data)}</SearchableTree>
        </DialogLayout>
    }
    onSearch(keysToExpand: string[]): void {
        if (this.state.searchText)
            this.setState({data: this.buildTree(this.state.initialData, null, l => _.includes(keysToExpand, l.key) || _.includes(l.name.toLowerCase(), this.state.searchText!.toLowerCase()))})
        else
            this.setState({data: this.buildTree(this.state.initialData, null)})
    }
    private onSelect(node: any) {
        const result: ClassifierReference[] = [];
        let r : ClassifierReference | null = this.state.parentMap[node.props.data.key]
        while(r) {
            result.unshift(r)
            r = r.parentId ? this.state.parentMap[r.parentId] : null
        }
        this.setState({selectedPath: result})
    }
}