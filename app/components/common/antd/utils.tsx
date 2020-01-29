import * as React from 'react'
import { Input } from 'antd'
export function CommonSorter(dataIndex: string): (a: any, b: any) => number {
    return (a, b) => a[dataIndex] > b[dataIndex] ? 1 : a[dataIndex] < b[dataIndex] ? -1 : 0;
}
export function CommonFilter(dataIndex: string): (value: string, record: any) => boolean {
    return (value: string, record: any) => {
        const data = record[dataIndex];
        if (data == null)
            return false;
        return data.toLowerCase().includes(value.toLowerCase())
    }
}
export function TextBoxDropdown({ setSelectedKeys, selectedKeys, confirm }: any) {
    return <Input.Search value={selectedKeys[0]} onChange={e => setSelectedKeys(e.target.value ? [e.target.value] : [])} onSearch={confirm} />
}