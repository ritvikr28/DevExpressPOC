import { useEffect, useCallback } from 'react';
import DxReportDesigner, { Callbacks, DesignerModelSettings, PreviewSettings, RequestOptions } from "devexpress-reporting-react/dx-report-designer";
import { SearchSettings } from 'devexpress-reporting-react/dx-report-viewer';
import { useSearchParams } from 'react-router-dom';
import { getToken } from '../../services/authService';
import './ReportDesigner.css';

// Extend window to include DevExpress namespace for TypeScript
declare global {
    interface Window {
        DevExpress?: {
            Analytics?: {
                Utils?: {
                    fetchSetup?: {
                        fetchSettings?: {
                            headers?: Record<string, string>;
                        };
                    };
                };
            };
        };
    }
}

export default function ReportDesigner(props: { hostUrl: string }) {
    const [searchParams] = useSearchParams();
    const getDesignerModelAction: string = 'DXXRD/GetDesignerModel';
    const getLocalizationAction: string = 'DXXRD/GetLocalization';
    const reportUrl: string = searchParams.get('reportUrl') ?? 'TestReport';
    
    // Configure DevExpress fetch settings with authorization header
    const onBeforeRender = useCallback(() => {
        const token = getToken();
        if (token && window.DevExpress?.Analytics?.Utils?.fetchSetup) {
            window.DevExpress.Analytics.Utils.fetchSetup.fetchSettings = {
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            };
        }
    }, []);

    // Set up the fetch interceptor when component mounts
    useEffect(() => {
        onBeforeRender();
    }, [onBeforeRender]);
    
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
