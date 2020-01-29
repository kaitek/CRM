import * as React from 'react';
import { Route } from 'react-router-dom';
import { BrowserRouter, NavLink } from 'react-router-dom';
import { App1 } from './index';
import { Home } from './Home';
import { Car } from './Car';

export const routes = <App1>
    <Route exact path='/' component={ Home } />
    <Route exact path='/Car' component={ Car } />
</App1>;