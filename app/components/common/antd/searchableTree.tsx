import * as React from 'react'
import { TreeProps, AntTreeNode, AntTreeNodeProps } from "antd/lib/tree";
import { Tree } from 'antd'
import * as _ from 'lodash'

interface SearchableTreeProps extends TreeProps {
    searchQuery?: string;
    data: SearchableTreeNodeData[], //it's neccesary to filter tree but it doesn't populate nodes
    onSearch?: (kyesToExpand:string[])=>void;
}

interface SearchableTreeState {
    searchQuery?: string;
    expandedKeys: string[],
}
export abstract class SearchableTreeNodeData {
    abstract get name(): string;
    abstract get key(): any;
    abstract get children(): SearchableTreeNodeData[];
}
export class SearchableTree extends React.Component<SearchableTreeProps, SearchableTreeState> {
    constructor(props: SearchableTreeProps) {
        super(props);
        this.state = {
            expandedKeys: [],
            searchQuery: ''
        }
    }
    render() {
        return <Tree {...this.props} 
        filterTreeNode={this.onFilterTreeNode.bind(this)}
        expandedKeys={this.state.expandedKeys}
        onExpand={expandedKeys => this.setState({ expandedKeys })}>
            {this.props.children}</Tree>
    }
    componentDidUpdate(prevProps: SearchableTreeProps, prevState: SearchableTreeState) {
        if (this.props.searchQuery !== prevProps.searchQuery) {
            const searchQuery = this.props.searchQuery;
            const kyesToExpand = searchQuery ? this.getKeysToExpand(this.props.data, searchQuery) : []
            this.setState({ searchQuery: this.props.searchQuery, expandedKeys: kyesToExpand})
            if (this.props.onSearch)
                this.props.onSearch(kyesToExpand); 
        }
    }

    onFilterTreeNode(node: AntTreeNode): boolean {
        return this.state.searchQuery ? this.eq(node.props.eventKey as string, this.state.searchQuery.toLowerCase()) : false
    }
    eq(name: string | undefined, query: string): boolean {
        return _.includes(name!.toLowerCase(), query.toLowerCase())
    }

    getKeysToExpand(nodes: SearchableTreeNodeData[], query: string): string[] {
        if (!nodes)
            return [];
        return nodes.filter(n => n.children && (n.children as AntTreeNodeProps[]).some(n => this.eq(n.key, query))).map(n => n.key as string).concat(...nodes.map(n => { 
            const kk = this.getKeysToExpand(n.children, query)
            if (kk.length)
                kk.push(n.key as string)
            return kk
        }))
    }
}