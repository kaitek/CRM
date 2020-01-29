import * as React from 'react';
import { Popconfirm } from 'antd';
import { tableContext } from './context';
import { connectionDelete } from './requests';
import { isEmpty } from '../../shared/common';

const _columns = [
    {
        title: 'Контакт',
        dataIndex: 'contact',
        width: '30%',
        //editable: true,
    },
    {
        title: 'Должность',
        dataIndex: 'connectionrole',
        editable: true,
    },
    {
        title: '',
        //width: '16%',
        dataIndex: 'operation',
        render: (text: any, record: any) => (
            <tableContext.Consumer>
                {({ dataSource, setValue }) => (
                    dataSource.length >= 1
                        ? (
                            <Popconfirm
                                title="Уверены?"
                                onConfirm={() => handleDelete(record.key, dataSource, setValue)}
                                cancelText="нет"
                                okText="да"
                            >
                                <a href="javascript:;">Удалить</a>
                            </Popconfirm>
                        ) : null
                )}
            </tableContext.Consumer>
        ),
    }];
export const columns = (handleSave: Function) => {
    return (
        _columns.map((col: any) => {
            return (!col.editable) ? col :
                {
                    ...col,
                    onCell: (record: any) => ({
                        record,
                        editable: col.editable,
                        dataIndex: col.dataIndex,
                        title: col.title,
                        handleSave: handleSave,
                    }),
                };
        })
    )
}
const handleDelete = (key: any, _: any[], setValue: Function) => {
    const data = [..._];
    let dataSource = data.filter(item => item.key !== key);
    let count = dataSource.length;
    setValue(dataSource, count);
    let record = data.find(item => item.key == key);
    !isEmpty(record) ? connectionDelete(record) : null;  
}