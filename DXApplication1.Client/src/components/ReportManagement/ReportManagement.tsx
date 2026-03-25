import { useState, useEffect } from 'react';
import './ReportManagement.css';

interface ReportInfo {
    name: string;
    storageLocation: string;
    lastModified: string | null;
}

interface ReportGenerationRequestItem {
    templateName: string;
    outputName: string;
    parameters?: Record<string, unknown>;
}

interface ReportGenerationResult {
    reportName: string;
    templateName: string;
    success: boolean;
    error?: string;
    localPath?: string;
    savedToAzure: boolean;
}

interface MultipleReportGenerationResult {
    totalRequested: number;
    successCount: number;
    failureCount: number;
    results: ReportGenerationResult[];
}

interface AzureStorageStatus {
    isEnabled: boolean;
}

export default function ReportManagement() {
    const [reports, setReports] = useState<ReportInfo[]>([]);
    const [azureStatus, setAzureStatus] = useState<AzureStorageStatus | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [successMessage, setSuccessMessage] = useState<string | null>(null);

    // Multiple report generation state
    const [generationRequests, setGenerationRequests] = useState<ReportGenerationRequestItem[]>([
        { templateName: '', outputName: '' }
    ]);
    const [saveToAzure, setSaveToAzure] = useState(false);
    const [generationResults, setGenerationResults] = useState<MultipleReportGenerationResult | null>(null);
    const [generating, setGenerating] = useState(false);

    useEffect(() => {
        loadData();
    }, []);

    const loadData = async () => {
        setLoading(true);
        setError(null);
        try {
            const [reportsRes, azureRes] = await Promise.all([
                fetch('/api/v1/reports'),
                fetch('/api/v1/reports/azure-status')
            ]);

            if (reportsRes.ok) {
                const reportsData = await reportsRes.json();
                setReports(reportsData);
            }

            if (azureRes.ok) {
                const azureData = await azureRes.json();
                setAzureStatus(azureData);
            }
        } catch (err) {
            setError('Failed to load data');
            console.error(err);
        } finally {
            setLoading(false);
        }
    };

    const saveToAzureStorage = async (reportName: string) => {
        try {
            const response = await fetch(`/api/v1/reports/${reportName}/save-to-azure`, {
                method: 'POST'
            });

            if (response.ok) {
                setSuccessMessage(`Report "${reportName}" saved to Azure Storage successfully`);
                loadData();
            } else {
                const errorData = await response.json();
                setError(errorData.error || 'Failed to save to Azure');
            }
        } catch (err) {
            setError('Failed to save to Azure');
            console.error(err);
        }

        setTimeout(() => {
            setSuccessMessage(null);
            setError(null);
        }, 5000);
    };

    const deleteFromAzure = async (reportName: string) => {
        if (!confirm(`Are you sure you want to delete "${reportName}" from Azure Storage?`)) {
            return;
        }

        try {
            const response = await fetch(`/api/v1/reports/${reportName}/azure`, {
                method: 'DELETE'
            });

            if (response.ok) {
                setSuccessMessage(`Report "${reportName}" deleted from Azure Storage`);
                loadData();
            } else {
                const errorData = await response.json();
                setError(errorData.error || 'Failed to delete from Azure');
            }
        } catch (err) {
            setError('Failed to delete from Azure');
            console.error(err);
        }

        setTimeout(() => {
            setSuccessMessage(null);
            setError(null);
        }, 5000);
    };

    const exportReport = async (reportName: string, format: string) => {
        try {
            const response = await fetch(`/api/v1/reports/${reportName}/export?format=${format}&saveToAzure=${saveToAzure}`);

            if (response.ok) {
                const blob = await response.blob();
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = `${reportName}.${format}`;
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                window.URL.revokeObjectURL(url);
            } else {
                const errorData = await response.json();
                setError(errorData.error || 'Failed to export report');
            }
        } catch (err) {
            setError('Failed to export report');
            console.error(err);
        }

        setTimeout(() => setError(null), 5000);
    };

    const addGenerationRequest = () => {
        setGenerationRequests([...generationRequests, { templateName: '', outputName: '' }]);
    };

    const removeGenerationRequest = (index: number) => {
        const newRequests = generationRequests.filter((_, i) => i !== index);
        setGenerationRequests(newRequests.length > 0 ? newRequests : [{ templateName: '', outputName: '' }]);
    };

    const updateGenerationRequest = (index: number, field: keyof ReportGenerationRequestItem, value: string) => {
        const newRequests = [...generationRequests];
        newRequests[index] = { ...newRequests[index], [field]: value };
        setGenerationRequests(newRequests);
    };

    const generateMultipleReports = async () => {
        const validRequests = generationRequests.filter(r => r.templateName && r.outputName);
        if (validRequests.length === 0) {
            setError('Please add at least one valid report generation request');
            return;
        }

        setGenerating(true);
        setGenerationResults(null);
        setError(null);

        try {
            const response = await fetch('/api/v1/reports/generate-multiple', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    reports: validRequests,
                    saveToAzure
                })
            });

            if (response.ok) {
                const results = await response.json();
                setGenerationResults(results);
                setSuccessMessage(`Generated ${results.successCount} of ${results.totalRequested} reports`);
                loadData();
            } else {
                const errorData = await response.json();
                setError(errorData.error || 'Failed to generate reports');
            }
        } catch (err) {
            setError('Failed to generate reports');
            console.error(err);
        } finally {
            setGenerating(false);
        }

        setTimeout(() => setSuccessMessage(null), 5000);
    };

    const getStorageLocationBadge = (location: string) => {
        switch (location) {
            case 'Local':
                return <span className="badge bg-primary">Local</span>;
            case 'Azure':
                return <span className="badge bg-info">Azure</span>;
            case 'Both':
                return <span className="badge bg-success">Local + Azure</span>;
            default:
                return <span className="badge bg-secondary">{location}</span>;
        }
    };

    if (loading) {
        return (
            <div className="container-fluid report-management">
                <div className="text-center py-5">
                    <div className="spinner-border" role="status">
                        <span className="visually-hidden">Loading...</span>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="container-fluid report-management">
            <h1>Report Management</h1>

            {/* Azure Status Banner */}
            <div className={`alert ${azureStatus?.isEnabled ? 'alert-success' : 'alert-warning'}`}>
                <strong>Azure Storage Status:</strong>{' '}
                {azureStatus?.isEnabled ? (
                    <span>✅ Connected - Reports will be synced to Azure Blob Storage</span>
                ) : (
                    <span>⚠️ Not configured - Set AzureStorage:ConnectionString in appsettings.json to enable</span>
                )}
            </div>

            {/* Messages */}
            {error && <div className="alert alert-danger">{error}</div>}
            {successMessage && <div className="alert alert-success">{successMessage}</div>}

            {/* Reports List Section */}
            <div className="card mb-4">
                <div className="card-header d-flex justify-content-between align-items-center">
                    <h4 className="mb-0">Available Reports</h4>
                    <button className="btn btn-sm btn-outline-primary" onClick={loadData}>
                        🔄 Refresh
                    </button>
                </div>
                <div className="card-body">
                    {reports.length === 0 ? (
                        <p className="text-muted">No reports found. Create a report using the Report Designer.</p>
                    ) : (
                        <div className="table-responsive">
                            <table className="table table-hover">
                                <thead>
                                    <tr>
                                        <th>Report Name</th>
                                        <th>Storage Location</th>
                                        <th>Last Modified</th>
                                        <th>Actions</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {reports.map((report, index) => (
                                        <tr key={index}>
                                            <td>{report.name}</td>
                                            <td>{getStorageLocationBadge(report.storageLocation)}</td>
                                            <td>
                                                {report.lastModified
                                                    ? new Date(report.lastModified).toLocaleString()
                                                    : '-'}
                                            </td>
                                            <td>
                                                <div className="btn-group btn-group-sm">
                                                    {/* Export buttons */}
                                                    <button
                                                        className="btn btn-outline-secondary"
                                                        onClick={() => exportReport(report.name, 'pdf')}
                                                        title="Export as PDF"
                                                    >
                                                        📄 PDF
                                                    </button>
                                                    <button
                                                        className="btn btn-outline-secondary"
                                                        onClick={() => exportReport(report.name, 'xlsx')}
                                                        title="Export as Excel"
                                                    >
                                                        📊 Excel
                                                    </button>

                                                    {/* Azure actions */}
                                                    {azureStatus?.isEnabled && (
                                                        <>
                                                            {(report.storageLocation === 'Local') && (
                                                                <button
                                                                    className="btn btn-outline-info"
                                                                    onClick={() => saveToAzureStorage(report.name)}
                                                                    title="Save to Azure"
                                                                >
                                                                    ☁️ Save to Azure
                                                                </button>
                                                            )}
                                                            {(report.storageLocation === 'Azure' || report.storageLocation === 'Both') && (
                                                                <button
                                                                    className="btn btn-outline-danger"
                                                                    onClick={() => deleteFromAzure(report.name)}
                                                                    title="Delete from Azure"
                                                                >
                                                                    🗑️ Delete from Azure
                                                                </button>
                                                            )}
                                                        </>
                                                    )}
                                                </div>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    )}
                </div>
            </div>

            {/* Multiple Report Generation Section */}
            <div className="card mb-4">
                <div className="card-header">
                    <h4 className="mb-0">Generate Multiple Reports</h4>
                </div>
                <div className="card-body">
                    <p className="text-muted mb-3">
                        Create multiple reports from templates. Each report can have different output names
                        and can optionally be saved to Azure Storage.
                    </p>

                    {/* Generation Requests */}
                    {generationRequests.map((request, index) => (
                        <div key={index} className="row mb-3 align-items-center">
                            <div className="col-md-4">
                                <select
                                    className="form-select"
                                    value={request.templateName}
                                    onChange={(e) => updateGenerationRequest(index, 'templateName', e.target.value)}
                                >
                                    <option value="">Select Template...</option>
                                    {reports.map((report) => (
                                        <option key={report.name} value={report.name}>
                                            {report.name}
                                        </option>
                                    ))}
                                </select>
                            </div>
                            <div className="col-md-1 text-center">
                                →
                            </div>
                            <div className="col-md-4">
                                <input
                                    type="text"
                                    className="form-control"
                                    placeholder="Output Report Name"
                                    value={request.outputName}
                                    onChange={(e) => updateGenerationRequest(index, 'outputName', e.target.value)}
                                />
                            </div>
                            <div className="col-md-2">
                                <button
                                    className="btn btn-outline-danger"
                                    onClick={() => removeGenerationRequest(index)}
                                    title="Remove"
                                >
                                    ✕ Remove
                                </button>
                            </div>
                        </div>
                    ))}

                    <div className="d-flex gap-2 mb-3">
                        <button className="btn btn-outline-secondary" onClick={addGenerationRequest}>
                            + Add Another Report
                        </button>
                    </div>

                    {/* Options */}
                    <div className="form-check mb-3">
                        <input
                            type="checkbox"
                            className="form-check-input"
                            id="saveToAzureCheck"
                            checked={saveToAzure}
                            onChange={(e) => setSaveToAzure(e.target.checked)}
                            disabled={!azureStatus?.isEnabled}
                        />
                        <label className="form-check-label" htmlFor="saveToAzureCheck">
                            Save generated reports to Azure Storage
                            {!azureStatus?.isEnabled && <span className="text-muted"> (Azure not configured)</span>}
                        </label>
                    </div>

                    {/* Generate Button */}
                    <button
                        className="btn btn-primary"
                        onClick={generateMultipleReports}
                        disabled={generating}
                    >
                        {generating ? (
                            <>
                                <span className="spinner-border spinner-border-sm me-2" role="status"></span>
                                Generating...
                            </>
                        ) : (
                            '🚀 Generate Reports'
                        )}
                    </button>

                    {/* Generation Results */}
                    {generationResults && (
                        <div className="mt-4">
                            <h5>Generation Results</h5>
                            <div className="alert alert-info">
                                <strong>Summary:</strong> {generationResults.successCount} of {generationResults.totalRequested} reports generated successfully
                                {generationResults.failureCount > 0 && (
                                    <span className="text-danger"> ({generationResults.failureCount} failed)</span>
                                )}
                            </div>
                            <table className="table table-sm">
                                <thead>
                                    <tr>
                                        <th>Report Name</th>
                                        <th>Template</th>
                                        <th>Status</th>
                                        <th>Azure</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {generationResults.results.map((result, index) => (
                                        <tr key={index}>
                                            <td>{result.reportName}</td>
                                            <td>{result.templateName}</td>
                                            <td>
                                                {result.success ? (
                                                    <span className="badge bg-success">✓ Success</span>
                                                ) : (
                                                    <span className="badge bg-danger" title={result.error}>
                                                        ✗ Failed
                                                    </span>
                                                )}
                                            </td>
                                            <td>
                                                {result.savedToAzure ? (
                                                    <span className="badge bg-info">☁️ Saved</span>
                                                ) : (
                                                    <span className="badge bg-secondary">-</span>
                                                )}
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}
