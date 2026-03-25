import DxReportViewer, { ProgressBarSettings, RequestOptions } from 'devexpress-reporting-react/dx-report-viewer';
import { useSearchParams } from 'react-router-dom';
import './ReportViewer.css';

export default function ReportViewer(props: { hostUrl: string }) {
    const [searchParams] = useSearchParams();
    const reportUrl: string = searchParams.get('reportUrl') ?? 'TestReport';
    const invokeAction: string = '/DXXRDV';
    const getLocalizationAction: string = `${invokeAction}/GetLocalization`
    return (
        <DxReportViewer reportUrl={reportUrl} height="calc(100vh - 90px)" developmentMode={true}>
            <RequestOptions invokeAction={invokeAction} getLocalizationAction={getLocalizationAction} host={props.hostUrl}></RequestOptions>
            <ProgressBarSettings position="BottomLeft"></ProgressBarSettings>
        </DxReportViewer>
    );
}
