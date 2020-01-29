import * as React from 'react';
import { connectionRetrieve, connectionCreate } from './requests';
import { isEmpty } from '../../shared/common'
import { tableContext } from './context';
import AddButton from './addButton';
import TableContainer from './tableContainer';


export class EditableTable extends React.Component<any, any> {
    handleSave = (row: any) => {
        const newData = [...this.state.dataSource];
        const index = newData.findIndex(item => row.key === item.key);
        const item = newData[index];
        newData.splice(index, 1, {
            ...item,
            ...row,
        });
        this.setState({ dataSource: newData });
    }

    insideLookupSave = (row: any) => {
        const { count, dataSource } = this.state;
        connectionCreate(row).then((res: any) => {
            row.key = !isEmpty(res) ? res.id : "";
            this.setState({
                dataSource: [...dataSource, row],
                count: count + 1,
            });
        });        
    }

    setValue = (dataSource: any[], count: number) => {
        this.setState({
            ['dataSource']: dataSource,
            ['count']: count
        });
    };

    componentDidMount() {
        let res = connectionRetrieve();
        let data = !isEmpty(res) && res!.length > 0 ? res : [];
        data!.length > 0 ? this.setState({ dataSource: data, count: data!.length }) : null;
    }

    state = {
        dataSource: new Array,
        count: 0,
        setValue: this.setValue,
    };

    render() {
        return (
            <>
                <tableContext.Provider value={this.state} >
                    <AddButton
                        insideLookupSave={this.insideLookupSave}
                    />
                    <TableContainer
                        handleSave={this.handleSave}
                    />
                </tableContext.Provider>
            </>
        );
    }
}
