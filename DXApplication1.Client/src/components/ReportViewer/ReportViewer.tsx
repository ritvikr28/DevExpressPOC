import { useEffect, useCallback } from 'react';
import DxReportViewer, { ProgressBarSettings, RequestOptions } from 'devexpress-reporting-react/dx-report-viewer';
import { useSearchParams } from 'react-router-dom';
import { getToken } from '../../services/authService';
import './ReportViewer.css';

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

export default function ReportViewer(props: { hostUrl: string }) {
    const [searchParams] = useSearchParams();
    const reportUrl: string = searchParams.get('reportUrl') ?? 'TestReport';
    const invokeAction: string = '/DXXRDV';
    const getLocalizationAction: string = `${invokeAction}/GetLocalization`;
    
    // Configure DevExpress fetch settings with authorization header
    const setupFetchHeaders = useCallback(() => {
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
        setupFetchHeaders();
    }, [setupFetchHeaders]);
    
    return (
        <DxReportViewer reportUrl={reportUrl} height="calc(100vh - 90px)" developmentMode={true}>
            <RequestOptions invokeAction={invokeAction} getLocalizationAction={getLocalizationAction} host={props.hostUrl} />
            <ProgressBarSettings position="BottomLeft"></ProgressBarSettings>
        </DxReportViewer>
    );
}
