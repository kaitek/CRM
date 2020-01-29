import * as React from 'react';

export const tableContext = React.createContext({
    dataSource: new Array,
    count: 0,
    setValue: (dataSource: any[], count: number) => { },
});
export const EditableContext = React.createContext(null);