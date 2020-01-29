import * as React from 'react';
import { Input, Form } from 'antd';
import { viewLookup } from './viewLookup';
import { isEmpty } from '../../shared/common';
import { connectionUpdateRole } from './requests';
import { EditableContext } from './context';

interface Values {
    connectionrole: string,
    contact: string,
    ownId: string
}

const FormItem = Form.Item;
const Search = Input.Search;

export class EditableCell extends React.Component<any, any> {
    constructor(props: any) {
        super(props);
        this.state = {}
    }
    save = (form: any, obj: { id: string, name: string }, e: any) => {
        const { record, handleSave } = this.props;
        form.validateFields((error: any, values: Values) => {
            if (error && error[e.currentTarget.id])
                return;
            if (!isEmpty(obj.id) && obj.id != record['roleId']) {
                record['roleId'] = obj.id;
                record['connectionrole'] = obj.name;
                connectionUpdateRole(record);
            }
            handleSave({ ...record, ...values });
        });
    }

    render() {
        const { editable, dataIndex, title, record,
            index, handleSave, ...restProps } = this.props;
        return (
            <td {...restProps}>
                {editable ? (
                    <EditableContext.Consumer>
                        {(form: any) => {
                            return (
                                <FormItem style={{ marginBottom: 0, marginTop: 0 }}>
                                    {form.getFieldDecorator(dataIndex, {
                                        rules: [{
                                            required: true,
                                            message: 'обязательное поле',
                                        }],
                                        initialValue: record[dataIndex],
                                    })(dataIndex == 'connectionrole' ?
                                        <Search
                                            onSearch={(val, e) => viewLookup(val, dataIndex, form, this.save, e)}
                                            //onBlur={e => this.save(form,e)}
                                            style={{ margin: 0 }}
                                        /> :
                                        <div className="editable-cell-text-wrapper">
                                            {record[dataIndex] || ' '}
                                        </div>
                                    )}
                                </FormItem>);
                        }}
                    </EditableContext.Consumer>
                ) : restProps.children}
            </td>
        );
    }
}