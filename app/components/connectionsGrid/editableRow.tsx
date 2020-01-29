import { Form, Input } from 'antd';
import * as React from 'react';
import { EditableContext } from './context';

const EditableRow = ({ form, index, ...props }: any) => (
    <EditableContext.Provider value={form}>
        <tr {...props} />
    </EditableContext.Provider>
);
export const FormItem = Form.Item;

export const EditableFormRow = Form.create()(EditableRow);