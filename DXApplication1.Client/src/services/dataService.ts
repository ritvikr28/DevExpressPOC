/**
 * Data service for managing data sources, schemas, and fetching data
 * Supports multiple data sources and column selection
 */

import { getAuthHeaders } from './authService';

const API_BASE = '/api/v1';

/**
 * Column metadata for a data source
 */
export interface ColumnMetadata {
    name: string;
    type: string;
}

/**
 * Data source schema with columns (no data)
 */
export interface DataSourceSchema {
    name: string;
    columns: ColumnMetadata[];
}

/**
 * List of available data sources with their schemas
 */
export interface DataSourcesListResponse {
    dataSources: DataSourceSchema[];
}

/**
 * Request for fetching data from a specific source
 */
export interface DataSourceRequest {
    dataSourceName: string;
    columns?: string[];
}

/**
 * Request for fetching data from multiple sources
 */
export interface MultiSourceDataRequest {
    sources: DataSourceRequest[];
}

/**
 * Result from fetching a single data source
 */
export interface DataSourceResult {
    dataSourceName: string;
    data: Record<string, unknown>[];
    error?: string;
}

/**
 * Response from fetching multiple data sources
 */
export interface MultiSourceDataResponse {
    results: DataSourceResult[];
}

/**
 * Gets all available data sources with their schemas (columns only, no data)
 */
export const getDataSources = async (): Promise<DataSourcesListResponse> => {
    const response = await fetch(`${API_BASE}/data/sources`, {
        method: 'GET',
        headers: {
            ...getAuthHeaders(),
            'Content-Type': 'application/json',
        },
    });

    if (!response.ok) {
        throw new Error(`Failed to fetch data sources: ${response.statusText}`);
    }

    return response.json();
};

/**
 * Gets the schema (columns) for a specific data source without loading data
 */
export const getDataSourceSchema = async (dataSourceName: string): Promise<DataSourceSchema> => {
    const response = await fetch(`${API_BASE}/data/schema?dataSourceName=${encodeURIComponent(dataSourceName)}`, {
        method: 'GET',
        headers: {
            ...getAuthHeaders(),
            'Content-Type': 'application/json',
        },
    });

    if (!response.ok) {
        throw new Error(`Failed to fetch schema for ${dataSourceName}: ${response.statusText}`);
    }

    return response.json();
};

/**
 * Gets data from a single data source with optional column selection
 */
export const getData = async (dataSourceName: string, columns?: string[]): Promise<Record<string, unknown>[]> => {
    let url = `${API_BASE}/data?dataSourceName=${encodeURIComponent(dataSourceName)}`;
    
    if (columns && columns.length > 0) {
        const columnParams = columns.map(c => `columns=${encodeURIComponent(c)}`).join('&');
        url += `&${columnParams}`;
    }

    const response = await fetch(url, {
        method: 'GET',
        headers: {
            ...getAuthHeaders(),
            'Content-Type': 'application/json',
        },
    });

    if (!response.ok) {
        throw new Error(`Failed to fetch data from ${dataSourceName}: ${response.statusText}`);
    }

    return response.json();
};

/**
 * Gets data from multiple data sources with column selection for each
 * 
 * @example
 * const result = await getMultiSourceData({
 *   sources: [
 *     { dataSourceName: 'Pupil', columns: ['PupilId', 'FirstName'] },
 *     { dataSourceName: 'Staff', columns: ['StaffId', 'Role'] }
 *   ]
 * });
 */
export const getMultiSourceData = async (request: MultiSourceDataRequest): Promise<MultiSourceDataResponse> => {
    const response = await fetch(`${API_BASE}/data/multi`, {
        method: 'POST',
        headers: {
            ...getAuthHeaders(),
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
    });

    if (!response.ok) {
        throw new Error(`Failed to fetch multi-source data: ${response.statusText}`);
    }

    return response.json();
};

/**
 * Selection state for columns across multiple data sources
 */
export interface DataSourceSelection {
    dataSourceName: string;
    selectedColumns: string[];
}

/**
 * Helper class to manage column selection state
 */
export class DataSelectionManager {
    private selections: Map<string, Set<string>> = new Map();

    /**
     * Add a data source with its available columns
     */
    addDataSource(dataSourceName: string): void {
        if (!this.selections.has(dataSourceName)) {
            this.selections.set(dataSourceName, new Set());
        }
    }

    /**
     * Toggle column selection for a data source
     */
    toggleColumn(dataSourceName: string, columnName: string): void {
        const columns = this.selections.get(dataSourceName);
        if (!columns) {
            this.selections.set(dataSourceName, new Set([columnName]));
            return;
        }

        if (columns.has(columnName)) {
            columns.delete(columnName);
        } else {
            columns.add(columnName);
        }
    }

    /**
     * Set all selected columns for a data source
     */
    setColumns(dataSourceName: string, columns: string[]): void {
        this.selections.set(dataSourceName, new Set(columns));
    }

    /**
     * Clear all selections for a data source
     */
    clearDataSource(dataSourceName: string): void {
        this.selections.delete(dataSourceName);
    }

    /**
     * Clear all selections
     */
    clearAll(): void {
        this.selections.clear();
    }

    /**
     * Get selected columns for a data source
     */
    getSelectedColumns(dataSourceName: string): string[] {
        return Array.from(this.selections.get(dataSourceName) || []);
    }

    /**
     * Check if a column is selected
     */
    isColumnSelected(dataSourceName: string, columnName: string): boolean {
        return this.selections.get(dataSourceName)?.has(columnName) || false;
    }

    /**
     * Get all selections as a request object
     */
    toRequest(): MultiSourceDataRequest {
        const sources: DataSourceRequest[] = [];
        
        for (const [dataSourceName, columns] of this.selections) {
            if (columns.size > 0) {
                sources.push({
                    dataSourceName,
                    columns: Array.from(columns),
                });
            }
        }

        return { sources };
    }

    /**
     * Get all selections as a list
     */
    getSelections(): DataSourceSelection[] {
        const result: DataSourceSelection[] = [];
        
        for (const [dataSourceName, columns] of this.selections) {
            result.push({
                dataSourceName,
                selectedColumns: Array.from(columns),
            });
        }

        return result;
    }

    /**
     * Check if any columns are selected
     */
    hasSelections(): boolean {
        for (const columns of this.selections.values()) {
            if (columns.size > 0) return true;
        }
        return false;
    }
}
