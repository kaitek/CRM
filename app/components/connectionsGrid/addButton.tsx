import * as React from 'react';
import { tableContext } from './context'
import { Button } from 'antd';
import { viewLookup } from './viewLookup';

interface Props {
    insideLookupSave: Function
}

const AddButton = (props: Props) => {
    const { insideLookupSave } = props;
    return (
        <tableContext.Consumer>
            {({ dataSource }) => (
                <Button
                    onClick={() => handleAdd(insideLookupSave)}
                    type="primary"
                    style={dataSource.length > 0 ?
                        { marginBottom: -7, position: 'absolute', zIndex: 10 } :
                        { marginBottom: 7, position: 'inherit', zIndex: 10 }
                    }
                >
                    Добавить
                    </Button>
            )}
        </tableContext.Consumer>
    );
}

const handleAdd = (insideLookupSave: Function) => {
    viewLookup("", "contact", null, undefined, null, insideLookupSave)
}
export default AddButton;
