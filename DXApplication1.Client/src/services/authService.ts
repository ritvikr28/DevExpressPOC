/**
 * Authentication service for managing JWT tokens and auth state
 */

export interface LoginRequest {
    username: string;
    password: string;
}

export interface LoginResponse {
    token: string;
    expiration: string;
    username: string;
    roles: string[];
}

export interface User {
    username: string;
    roles: string[];
    isActive: boolean;
}

const TOKEN_KEY = 'auth_token';
const USER_KEY = 'auth_user';

/**
 * Store authentication token in local storage
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
 * Store user data in local storage
 */
export const setUser = (user: Omit<LoginResponse, 'token'>): void => {
    localStorage.setItem(USER_KEY, JSON.stringify(user));
};

/**
 * Get user data from local storage
 */
export const getUser = (): Omit<LoginResponse, 'token'> | null => {
    const userJson = localStorage.getItem(USER_KEY);
    if (!userJson) return null;
    try {
        return JSON.parse(userJson);
    } catch {
        return null;
    }
};

/**
 * Remove user data from local storage
 */
export const removeUser = (): void => {
    localStorage.removeItem(USER_KEY);
};

/**
 * Check if user is authenticated
 */
export const isAuthenticated = (): boolean => {
    const token = getToken();
    if (!token) return false;
    
    // Check if token is expired
    try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        const expiry = payload.exp * 1000; // Convert to milliseconds
        return Date.now() < expiry;
    } catch {
        return false;
    }
};

/**
 * Login and store authentication data
 */
export const login = async (credentials: LoginRequest): Promise<LoginResponse> => {
    const response = await fetch('/api/v1/auth/login', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(credentials),
    });

    if (!response.ok) {
        const error = await response.json().catch(() => ({ error: 'Login failed' }));
        throw new Error(error.error || 'Login failed');
    }

    const data: LoginResponse = await response.json();
    setToken(data.token);
    setUser({
        username: data.username,
        expiration: data.expiration,
        roles: data.roles,
    });

    return data;
};

/**
 * Logout and clear authentication data
 */
export const logout = (): void => {
    removeToken();
    removeUser();
};

/**
 * Verify current token with server
 */
export const verifyToken = async (): Promise<boolean> => {
    const token = getToken();
    if (!token) return false;

    try {
        const response = await fetch('/api/v1/auth/verify', {
            headers: {
                'Authorization': `Bearer ${token}`,
            },
        });
        return response.ok;
    } catch {
        return false;
    }
};

/**
 * Get current user info from server
 */
export const getCurrentUser = async (): Promise<User | null> => {
    const token = getToken();
    if (!token) return null;

    try {
        const response = await fetch('/api/v1/auth/me', {
            headers: {
                'Authorization': `Bearer ${token}`,
            },
        });

        if (!response.ok) return null;
        return await response.json();
    } catch {
        return null;
    }
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
 * Check if user has a specific role
 */
export const hasRole = (role: string): boolean => {
    const user = getUser();
    if (!user) return false;
    return user.roles.includes(role);
};

/**
 * Check if user has any of the specified roles
 */
export const hasAnyRole = (roles: string[]): boolean => {
    const user = getUser();
    if (!user) return false;
    return roles.some(role => user.roles.includes(role));
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

    // If unauthorized, clear auth data
    if (response.status === 401) {
        logout();
        window.location.href = '/login';
    }

    return response;
};

export const AppRoles = {
    Admin: 'Admin',
    ReportViewer: 'ReportViewer',
    ReportEditor: 'ReportEditor',
} as const;
