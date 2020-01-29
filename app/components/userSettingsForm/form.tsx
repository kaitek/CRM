import * as React from 'react'
import { Form, Button, Input, Icon } from 'antd'
import { FormComponentProps } from 'antd/lib/form';
import * as _ from 'lodash'

export interface SubmitEventArgs {
    context?: any,
    values: { [key: string]: any};
}
interface LoginPasswordFormProps extends FormComponentProps {
    onSubmit: (e: SubmitEventArgs) => void;
    context?: any;
}
class LoginPasswordForm extends React.Component<LoginPasswordFormProps> {
    constructor(props: any) {
        super(props);
    }
    componentDidMount() {
        // To disabled submit button at the beginning.
        this.props.form.validateFields();
    }
    onSubmit(e: React.FormEvent<any>) {
        e.preventDefault();
        this.props.form.validateFields((errors, values) => {
            if (errors)
                return;
            this.props.onSubmit({
                context: this.props.context,
                values: values
            })
        })
    }
    render() {
        const { getFieldDecorator, getFieldsError } = this.props.form;
        return <Form onSubmit={this.onSubmit.bind(this)}>
            <Form.Item label="Логин">
            {getFieldDecorator('login', {
                            rules: [{
                                required: true,
                                message: 'Логин обязателен'
                            }], 
                })(<Input prefix={<Icon type="user" />} placeholder="Логин" autoComplete="new-password"/>)}
            </Form.Item>
            <Form.Item label="Пароль">
            {getFieldDecorator('password', {
                            rules: [{
                                required: true,
                                message: 'Пароль обязателен'
                            }],
                })(<Input prefix={<Icon type="lock" />} type="password" placeholder="Пароль" autoComplete="new-password"/>)}
            </Form.Item>
            <Button type="primary" htmlType="submit" disabled={_(getFieldsError()).values().some(f => f != null)}>Обновить</Button>
        </Form>
    }
}
export const WrappedLoginPasswordForm = Form.create()(LoginPasswordForm)