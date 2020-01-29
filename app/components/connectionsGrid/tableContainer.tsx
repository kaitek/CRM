import * as React from 'react';
import { tableContext } from './context';
import { Table } from 'antd';
import { columns } from './columns';
import { EditableCell } from './editableCell';
import { EditableFormRow } from './editableRow';

interface Props {
    handleSave: Function
}

const TableContainer = (props: Props) => {
    const { handleSave } = props;
    const components = {
        body: {
            row: EditableFormRow,
            cell: EditableCell,
        },
    };
    return (
        <tableContext.Consumer>
            {({ dataSource }) => (
                <Table
                    size='small'
                    components={components}
                    rowClassName={() => 'editable-row'}
                    bordered
                    dataSource={dataSource}
                    columns={columns(handleSave)}
                    pagination={{
                            position: "top", size: "small", pageSize: 3,
                            simple: true, defaultCurrent: 1
                        }}
                />
            )}
        </tableContext.Consumer>
    );
}
export default TableContainer;