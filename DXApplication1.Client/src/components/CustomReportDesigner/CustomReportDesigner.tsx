import { useState, useEffect, useCallback } from 'react';
import DxReportDesigner, { Callbacks, DesignerModelSettings, PreviewSettings, RequestOptions } from "devexpress-reporting-react/dx-report-designer";
import { SearchSettings } from 'devexpress-reporting-react/dx-report-viewer';
import { useSearchParams } from 'react-router-dom';
import { getToken, getAuthHeaders } from '../../services/authService';
import './CustomReportDesigner.css';

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

interface ColumnSchema {
    Name: string;
    Type: string;
}

export default function CustomReportDesigner(props: { hostUrl: string }) {
    const [searchParams] = useSearchParams();
    const [dataSources, setDataSources] = useState<string[]>([]);
    const [selectedDataSource, setSelectedDataSource] = useState<string>('');
    const [schema, setSchema] = useState<ColumnSchema[]>([]);
    const [selectedColumns, setSelectedColumns] = useState<string[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const getDesignerModelAction: string = 'DXXRD/GetDesignerModel';
    const getLocalizationAction: string = 'DXXRD/GetLocalization';
    const reportUrl: string = searchParams.get('reportUrl') ?? 'TestReport';

    // Fetch available data sources on component mount
    useEffect(() => {
        const fetchDataSources = async () => {
            try {
                const response = await fetch(`${props.hostUrl}api/v1/datasources`, {
                    headers: getAuthHeaders()
                });
                if (!response.ok) throw new Error('Failed to fetch data sources');
                const data = await response.json();
                setDataSources(data);
            } catch (err) {
                setError(err instanceof Error ? err.message : 'Failed to fetch data sources');
            }
        };
        fetchDataSources();
    }, [props.hostUrl]);

    // Fetch schema when data source is selected
    useEffect(() => {
        if (!selectedDataSource) {
            setSchema([]);
            setSelectedColumns([]);
            return;
        }

        const fetchSchema = async () => {
            setIsLoading(true);
            setError(null);
            try {
                const response = await fetch(`${props.hostUrl}api/v1/schema?dataSourceName=${encodeURIComponent(selectedDataSource)}`, {
                    headers: getAuthHeaders()
                });
                if (!response.ok) throw new Error('Failed to fetch schema');
                const data: ColumnSchema[] = await response.json();
                setSchema(data);
                setSelectedColumns([]); // Reset selected columns when data source changes
            } catch (err) {
                setError(err instanceof Error ? err.message : 'Failed to fetch schema');
                setSchema([]);
            } finally {
                setIsLoading(false);
            }
        };
        fetchSchema();
    }, [selectedDataSource, props.hostUrl]);

    // Handle data source selection
    const handleDataSourceChange = (event: React.ChangeEvent<HTMLSelectElement>) => {
        setSelectedDataSource(event.target.value);
    };

    // Handle column selection for report fields
    const handleColumnToggle = (columnName: string) => {
        setSelectedColumns(prev =>
            prev.includes(columnName)
                ? prev.filter(c => c !== columnName)
                : [...prev, columnName]
        );
    };

    // Select all columns
    const handleSelectAll = () => {
        setSelectedColumns(schema.map(col => col.Name));
    };

    // Clear all columns
    const handleClearAll = () => {
        setSelectedColumns([]);
    };

    // Build the preview URL with selected columns
    const getPreviewDataUrl = useCallback(() => {
        if (!selectedDataSource || selectedColumns.length === 0) return '';
        const columnsParam = selectedColumns.map(c => `columns=${encodeURIComponent(c)}`).join('&');
        return `${props.hostUrl}api/v1/data?dataSourceName=${encodeURIComponent(selectedDataSource)}&${columnsParam}`;
    }, [selectedDataSource, selectedColumns, props.hostUrl]);

    // BeforeRender callback - Configure DevExpress fetch settings with authorization header
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

    // Custom callback for when preview is requested
    const onPreviewClick = useCallback(() => {
        // Custom preview handling - logs the URL that would be called
        const dataUrl = getPreviewDataUrl();
        if (dataUrl) {
            console.log('Preview data URL:', dataUrl);
        }
    }, [getPreviewDataUrl]);

    const getTypeIcon = (type: string): string => {
        switch (type.toLowerCase()) {
            case 'int':
            case 'decimal':
            case 'number':
                return '🔢';
            case 'string':
                return '📝';
            case 'date':
            case 'datetime':
                return '📅';
            case 'bool':
            case 'boolean':
                return '✓';
            default:
                return '📄';
        }
    };

    return (
        <div className="custom-report-designer-container">
            {/* Data Source Selection Panel */}
            <div className="datasource-panel">
                <h3>📊 Data Source Configuration</h3>

                {error && <div className="error-message">{error}</div>}

                <div className="datasource-selector">
                    <label htmlFor="datasource-select">Select Data Source:</label>
                    <select
                        id="datasource-select"
                        value={selectedDataSource}
                        onChange={handleDataSourceChange}
                        disabled={isLoading}
                    >
                        <option value="">-- Select a Data Source --</option>
                        {dataSources.map((ds) => (
                            <option key={ds} value={ds}>{ds}</option>
                        ))}
                    </select>
                </div>

                {/* Schema / Column List Panel */}
                {selectedDataSource && (
                    <div className="schema-panel">
                        <div className="schema-header">
                            <h4>📋 Available Columns</h4>
                            <div className="schema-actions">
                                <button onClick={handleSelectAll} disabled={schema.length === 0}>
                                    Select All
                                </button>
                                <button onClick={handleClearAll} disabled={selectedColumns.length === 0}>
                                    Clear All
                                </button>
                            </div>
                        </div>

                        {isLoading ? (
                            <div className="loading">Loading schema...</div>
                        ) : (
                            <div className="column-list">
                                {schema.map((col) => (
                                    <label
                                        key={col.Name}
                                        className={`column-item ${selectedColumns.includes(col.Name) ? 'selected' : ''}`}
                                        draggable
                                        onDragStart={(e) => {
                                            e.dataTransfer.setData('text/plain', JSON.stringify({
                                                name: col.Name,
                                                type: col.Type,
                                                dataSource: selectedDataSource
                                            }));
                                            e.dataTransfer.effectAllowed = 'copy';
                                        }}
                                    >
                                        <span className="column-icon">{getTypeIcon(col.Type)}</span>
                                        <span className="column-name">{col.Name}</span>
                                        <span className="column-type">({col.Type})</span>
                                        <input
                                            type="checkbox"
                                            checked={selectedColumns.includes(col.Name)}
                                            onChange={() => handleColumnToggle(col.Name)}
                                            aria-label={`Select column ${col.Name}`}
                                        />
                                    </label>
                                ))}
                            </div>
                        )}

                        {/* Selected Columns Summary */}
                        {selectedColumns.length > 0 && (
                            <div className="selected-summary">
                                <strong>Selected: </strong>
                                {selectedColumns.join(', ')}
                            </div>
                        )}

                        {/* Preview Data URL Info */}
                        {selectedColumns.length > 0 && (
                            <div className="preview-info">
                                <strong>Preview API:</strong>
                                <code>{getPreviewDataUrl()}</code>
                            </div>
                        )}
                    </div>
                )}
            </div>

            {/* DevExpress Report Designer */}
            <div className="designer-panel">
                <DxReportDesigner
                    reportUrl={reportUrl}
                    height="calc(100vh - 90px)"
                    developmentMode={true}
                >
                    <RequestOptions
                        host={props.hostUrl}
                        getLocalizationAction={getLocalizationAction}
                        getDesignerModelAction={getDesignerModelAction}
                    />
                    <Callbacks
                        BeforeRender={onBeforeRender}
                        PreviewClick={onPreviewClick}
                    />
                    <DesignerModelSettings>
                        <PreviewSettings>
                            <SearchSettings searchEnabled={true} />
                        </PreviewSettings>
                    </DesignerModelSettings>
                </DxReportDesigner>
            </div>
        </div>
    );
}
