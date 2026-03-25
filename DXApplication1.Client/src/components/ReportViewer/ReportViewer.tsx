import DxReportViewer, { ProgressBarSettings, RequestOptions } from 'devexpress-reporting-react/dx-report-viewer';
import './ReportViewer.css';

export default function ReportViewer(props: { hostUrl: string }) {
    const reportUrl: string = 'TestReport';
    const invokeAction: string = '/DXXRDV';
    const getLocalizationAction: string = `${invokeAction}/GetLocalization`
    return (
        <DxReportViewer reportUrl={reportUrl} height="calc(100vh - 90px)" developmentMode={true}>
            <RequestOptions invokeAction={invokeAction} getLocalizationAction={getLocalizationAction} host={props.hostUrl}></RequestOptions>
            <ProgressBarSettings position="BottomLeft"></ProgressBarSettings>
        </DxReportViewer>
    );
}
