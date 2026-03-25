/**
 * Authentication service for managing JWT tokens from external STS provider
 * Tokens are obtained from: https://simsid-partner-stsserver.azurewebsites.net/
 */

const TOKEN_KEY = 'auth_token';

/**
 * Store authentication token in local storage
 * Call this after obtaining a token from the external STS provider
 */
export const setToken = (token: string): void => {
    localStorage.setItem(TOKEN_KEY, token);
};

/**
 * Get authentication token from local storage
 */
export const getToken = (): string | null => {
    return localStorage.getItem(TOKEN_KEY);
};

/**
 * Remove authentication token from local storage
 */
export const removeToken = (): void => {
    localStorage.removeItem(TOKEN_KEY);
};

/**
 * Check if user is authenticated (has a valid token)
 */
export const isAuthenticated = (): boolean => {
    const token = getToken();
    if (!token) return false;
    
    // Check if token is expired by parsing the JWT payload
    try {
        // Validate JWT structure: must have 3 parts separated by dots
        const parts = token.split('.');
        if (parts.length !== 3) {
            return false;
        }
        
        const payloadBase64 = parts[1];
        if (!payloadBase64) {
            return false;
        }
        
        const payload = JSON.parse(atob(payloadBase64));
        if (typeof payload.exp !== 'number') {
            return false;
        }
        
        const expiry = payload.exp * 1000; // Convert to milliseconds
        return Date.now() < expiry;
    } catch {
        return false;
    }
};

/**
 * Logout and clear authentication data
 */
export const logout = (): void => {
    removeToken();
};

/**
 * Create headers with authorization token
 */
export const getAuthHeaders = (): HeadersInit => {
    const token = getToken();
    if (!token) return {};
    return {
        'Authorization': `Bearer ${token}`,
    };
};

/**
 * Authenticated fetch wrapper that adds authorization header
 */
export const authFetch = async (
    url: string,
    options: RequestInit = {}
): Promise<Response> => {
    const token = getToken();
    
    const headers = new Headers(options.headers);
    if (token) {
        headers.set('Authorization', `Bearer ${token}`);
    }
    
    const response = await fetch(url, {
        ...options,
        headers,
    });

    // If unauthorized, clear auth data and let the caller handle navigation
    if (response.status === 401) {
        logout();
    }

    return response;
};
