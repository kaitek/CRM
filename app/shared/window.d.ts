declare global {
    interface Window {
        $: JQueryStatic,
        LMR: any;
        IsUSD: boolean;
        closeWindow(b: boolean): void;
    }
}
export default Window;