import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { Link, NavLink } from 'react-router-dom';
import { Layout, Menu, Icon } from 'antd';
const { Header, Content, Footer, Sider } = Layout;
const SubMenu = Menu.SubMenu;

export class AppMenu extends React.Component {
    render() {
        return (
            <div>
                <div className="logo" />
                <Menu theme='dark' defaultSelectedKeys={['1']} mode="vertical">
                    <Menu.Item key="1">
                        <NavLink to={'/'} activeClassName='active'>
                            <Icon type="pie-chart" />
                            <span>Домой</span>
                        </NavLink>
                    </Menu.Item>
                    <Menu.Item key="2">
                        <NavLink to={'/Car'} activeClassName='active'>
                            <Icon type="search" />
                            <span>Машины</span>
                        </NavLink>
                    </Menu.Item>
                </Menu>
            </div>
        );
    }
};

