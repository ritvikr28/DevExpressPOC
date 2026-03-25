import DxReportDesigner, { Callbacks, DesignerModelSettings, PreviewSettings, RequestOptions } from "devexpress-reporting-react/dx-report-designer";
import { SearchSettings } from 'devexpress-reporting-react/dx-report-viewer';
import './ReportDesigner.css';

export default function ReportDesigner(props: { hostUrl: string }) {
    const getDesignerModelAction: string = 'DXXRD/GetDesignerModel';
    const getLocalizationAction: string = 'DXXRD/GetLocalization'
    const reportUrl: string = 'TestReport';
    const onBeforeRender = () => { };
    return (
        <DxReportDesigner reportUrl={reportUrl} height="calc(100vh - 90px)" developmentMode={true}>
            <RequestOptions host={props.hostUrl} getLocalizationAction={getLocalizationAction} getDesignerModelAction={getDesignerModelAction} />
            <Callbacks BeforeRender={onBeforeRender}></Callbacks>
            <DesignerModelSettings>
                <PreviewSettings>
                    <SearchSettings searchEnabled={true}></SearchSettings>
                </PreviewSettings>
            </DesignerModelSettings>
        </DxReportDesigner>
    );
}
