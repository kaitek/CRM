import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { Link, RouteComponentProps } from 'react-router-dom';
import { AppMenu } from './AppMenu';
import { Layout, Menu, Breadcrumb, Icon, Avatar } from 'antd';

const SubMenu = Menu.SubMenu;

export class App1 extends React.Component {

    state = {
        collapsed: false,
    };

    componentWillMount() {
    }

    onCollapse = (collapsed: any) => {
        this.setState({ collapsed });
    }

    render() {
        return (
            <Layout style={{ minHeight: '100vh' }}>
                <Layout.Sider
                    collapsible
                    collapsed={this.state.collapsed}
                    onCollapse={this.onCollapse}
                >
                    <AppMenu />
                </Layout.Sider>
                <Layout>
                    <Layout.Content style={{ margin: '16px' }}>
                        <div style={{ padding: 24, background: '#fff', minHeight: 360 }}>
                            {this.props.children}
                        </div>
                    </Layout.Content>
                    <Layout.Footer style={{ textAlign: 'center' }}>
                        LMR ©2018
                    </Layout.Footer>
                </Layout>
            </Layout>
        );
    }
}