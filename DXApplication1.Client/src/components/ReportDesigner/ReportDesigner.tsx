import { useCallback, useMemo, useRef, useState } from 'react';
import DxReportDesigner, { Callbacks, DesignerModelSettings, PreviewSettings, RequestOptions, DxReportDesignerRef } from "devexpress-reporting-react/dx-report-designer";
import { SearchSettings } from 'devexpress-reporting-react/dx-report-viewer';
import { fetchSetup } from '@devexpress/analytics-core/analytics-utils';
import { useSearchParams, Link } from 'react-router-dom';
import { getToken } from '../../services/authService';
import './ReportDesigner.css';

// Height constants for the designer component
const NAVBAR_HEIGHT = 90; // Main navbar height in pixels
const BANNER_HEIGHT = 50; // Data sources banner height in pixels
const CUSTOM_TOOLBAR_HEIGHT = 50; // Custom toolbar height in pixels

// Action IDs for buttons to hide/disable
// DevExpress uses ActionId enum values for standard actions
const HIDDEN_ACTION_IDS = [
    'dxrd-preview',              // Hide built-in Preview (moved to custom location)
    'dxrd-addSqlDataSource',     // Hide SQL Data Source button
    'dxrd-addObjectDataSource',  // Hide Object Data Source button
    'dxrd-addJsonDataSource',    // Hide JSON Data Source button (if present)
    'dxrd-addFederatedDataSource', // Hide Federated Data Source button
    'dxrd-manageQueries',        // Hide Manage Queries button
    'dxrd-script-editor',        // Hide Script Editor
];

// Type definitions for DevExpress menu action customization
interface MenuAction {
    id: string;
    visible: boolean;
}

// DevExpress callback args interface with GetById method
interface CustomizeMenuActionsArgs {
    Actions: MenuAction[];
    GetById: (id: string) => MenuAction | undefined;
}

interface CustomizeMenuActionsEvent {
    sender: unknown;
    args: CustomizeMenuActionsArgs;
}

// Type definition for TabChanged event (DevExpress callback)
interface TabChangedEvent {
    Tab?: {
        context?: {
            designMode?: () => boolean;
        };
    };
}

export default function ReportDesigner(props: { hostUrl: string }) {
    const [searchParams] = useSearchParams();
    const getDesignerModelAction: string = 'DXXRD/GetDesignerModel';
    const getLocalizationAction: string = 'DXXRD/GetLocalization';
    const reportUrl: string = searchParams.get('reportUrl') ?? 'TestReport';
    
    // Reference to the designer for programmatic control
    const designerRef = useRef<DxReportDesignerRef>(null);
    const [isPreviewMode, setIsPreviewMode] = useState(false);
    
    // Parse selected data sources from query params (passed from DataSourceSelector)
    const selectedDataSources = useMemo(() => {
        const sourcesParam = searchParams.get('dataSources');
        return sourcesParam ? sourcesParam.split(',').filter(s => s.trim()) : [];
    }, [searchParams]);
    
    // Calculate designer height based on whether banner is shown and custom toolbar
    const designerHeight = useMemo(() => {
        let height = NAVBAR_HEIGHT + CUSTOM_TOOLBAR_HEIGHT;
        if (selectedDataSources.length > 0) {
            height += BANNER_HEIGHT;
        }
        return `calc(100vh - ${height}px)`;
    }, [selectedDataSources.length]);
    
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
    
    // Customize menu actions - hide/disable specific buttons
    // DevExpress uses { sender, args } pattern where args.GetById() retrieves actions
    const onCustomizeMenuActions = useCallback((event: CustomizeMenuActionsEvent) => {
        // Debug log to help identify the event structure
        console.log('CustomizeMenuActions event:', event);
        console.log('Event keys:', Object.keys(event || {}));
        
        const { args } = event;
        
        // Try GetById first (DevExpress preferred approach)
        if (args && typeof args.GetById === 'function') {
            console.log('Using GetById approach');
            HIDDEN_ACTION_IDS.forEach(actionId => {
                const action = args.GetById(actionId);
                if (action) {
                    console.log(`Hiding action: ${actionId}`);
                    action.visible = false;
                }
            });
        } 
        // Fallback: iterate over Actions array if GetById is not available
        else if (args && args.Actions && Array.isArray(args.Actions)) {
            console.log('Using args.Actions array approach');
            args.Actions.forEach((action: MenuAction) => {
                if (HIDDEN_ACTION_IDS.includes(action.id)) {
                    console.log(`Hiding action: ${action.id}`);
                    action.visible = false;
                }
            });
        }
        // Direct event.Actions fallback (older API style)
        else if ((event as any).Actions && Array.isArray((event as any).Actions)) {
            console.log('Using direct event.Actions approach');
            (event as any).Actions.forEach((action: MenuAction) => {
                if (HIDDEN_ACTION_IDS.includes(action.id)) {
                    console.log(`Hiding action: ${action.id}`);
                    action.visible = false;
                }
            });
        } else {
            console.warn('CustomizeMenuActions: Could not find Actions in event structure');
        }
    }, []);
    
    // Custom Preview button handler - triggers preview mode programmatically
    // Uses DevExpress API: instance() returns JSReportDesigner, ShowPreview() switches to preview mode
    const handlePreviewClick = useCallback(() => {
        if (designerRef.current) {
            try {
                // DevExpress API: instance() returns the underlying JSReportDesigner object
                const designerInstance = designerRef.current.instance();
                if (designerInstance) {
                    // DevExpress API: ShowPreview() method switches the designer to preview mode
                    designerInstance.ShowPreview();
                    setIsPreviewMode(true);
                }
            } catch (error) {
                console.error('Failed to trigger preview:', error);
            }
        }
    }, []);
    
    // DevExpress callback: fired when preview mode is entered (also triggered by our custom button)
    const onPreviewClick = useCallback(() => {
        setIsPreviewMode(true);
    }, []);
    
    // DevExpress callback: fired when tab changes - check if returning to designer
    const onTabChanged = useCallback((event: TabChangedEvent) => {
        // When tab changes, check if we're back in design mode
        try {
            const isDesignMode = event.Tab?.context?.designMode?.();
            if (isDesignMode !== undefined) {
                setIsPreviewMode(!isDesignMode);
            }
        } catch (error) {
            // Log error and fallback to design mode assumption
            console.warn('Failed to determine designer mode from tab change:', error);
            setIsPreviewMode(false);
        }
    }, []);
    
    return (
        <div className="report-designer-container">
            {/* Custom Toolbar with Preview button moved here */}
            <div className="custom-designer-toolbar">
                <div className="toolbar-section">
                    <span className="toolbar-title">Report Designer</span>
                    {isPreviewMode && (
                        <span className="badge bg-info ms-2">Preview Mode</span>
                    )}
                </div>
                <div className="toolbar-section toolbar-actions">
                    <button 
                        className="btn btn-primary preview-btn"
                        onClick={handlePreviewClick}
                        title="Preview Report"
                        disabled={isPreviewMode}
                    >
                        <i className="dx-icon dx-icon-preview"></i>
                        Preview
                    </button>
                </div>
            </div>
            
            {/* Show selected data sources banner if coming from DataSourceSelector */}
            {selectedDataSources.length > 0 && (
                <div className="selected-sources-banner alert alert-info mb-0">
                    <strong>Selected Data Sources:</strong>{' '}
                    {selectedDataSources.map((source) => (
                        <span key={source} className="badge bg-primary me-1">
                            {source}
                        </span>
                    ))}
                    <span className="ms-2 text-muted">
                        — These data sources are available in the Field List. Add them to your report, then click Preview to fetch data from all sources.
                    </span>
                    <Link to="/DataSourceSelector" className="btn btn-sm btn-outline-secondary ms-2">
                        Change Selection
                    </Link>
                </div>
            )}
            
            <DxReportDesigner 
                ref={designerRef}
                reportUrl={reportUrl} 
                height={designerHeight} 
                developmentMode={true}
            >
                <RequestOptions host={props.hostUrl} getLocalizationAction={getLocalizationAction} getDesignerModelAction={getDesignerModelAction} />
                <Callbacks 
                    BeforeRender={onBeforeRender}
                    CustomizeMenuActions={onCustomizeMenuActions}
                    PreviewClick={onPreviewClick}
                    TabChanged={onTabChanged}
                />
                <DesignerModelSettings>
                    <PreviewSettings>
                        <SearchSettings searchEnabled={true}></SearchSettings>
                    </PreviewSettings>
                </DesignerModelSettings>
            </DxReportDesigner>
        </div>
    );
}
