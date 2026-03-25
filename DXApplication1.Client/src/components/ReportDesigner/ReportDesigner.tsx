import { useCallback } from 'react';
import DxReportDesigner, { Callbacks, DesignerModelSettings, PreviewSettings, RequestOptions } from "devexpress-reporting-react/dx-report-designer";
import { SearchSettings } from 'devexpress-reporting-react/dx-report-viewer';
import { fetchSetup } from '@devexpress/analytics-core/analytics-utils';
import { useSearchParams } from 'react-router-dom';
import { getToken } from '../../services/authService';
import './ReportDesigner.css';

export default function ReportDesigner(props: { hostUrl: string }) {
    const [searchParams] = useSearchParams();
    const getDesignerModelAction: string = 'DXXRD/GetDesignerModel';
    const getLocalizationAction: string = 'DXXRD/GetLocalization';
    const reportUrl: string = searchParams.get('reportUrl') ?? 'TestReport';
    
    // BeforeRender fires before the DevExpress designer makes any HTTP requests,
    // so this is the correct place to ensure the Authorization header is set.
    const onBeforeRender = useCallback(() => {
        const token = getToken();
        if (token) {
            fetchSetup.fetchSettings = {
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            };
        }
    }, []);
    
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
