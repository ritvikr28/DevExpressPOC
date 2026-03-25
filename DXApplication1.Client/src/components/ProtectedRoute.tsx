import { Navigate, useLocation } from 'react-router-dom';
import { isAuthenticated, hasAnyRole } from '../services/authService';

interface ProtectedRouteProps {
    children: React.ReactNode;
    requiredRoles?: string[];
}

/**
 * Protected Route component that requires authentication and optional role checks
 */
export default function ProtectedRoute({ children, requiredRoles }: ProtectedRouteProps) {
    const location = useLocation();

    // Check if user is authenticated
    if (!isAuthenticated()) {
        // Redirect to login, saving the current location
        return <Navigate to="/login" state={{ from: location }} replace />;
    }

    // Check if user has required roles
    if (requiredRoles && requiredRoles.length > 0 && !hasAnyRole(requiredRoles)) {
        // User doesn't have required role, show access denied
        return (
            <div style={{ 
                display: 'flex', 
                justifyContent: 'center', 
                alignItems: 'center', 
                height: '100vh',
                flexDirection: 'column',
                gap: '16px'
            }}>
                <h1>Access Denied</h1>
                <p>You don't have permission to access this page.</p>
                <button 
                    onClick={() => window.history.back()}
                    style={{
                        padding: '10px 20px',
                        backgroundColor: '#007bff',
                        color: 'white',
                        border: 'none',
                        borderRadius: '4px',
                        cursor: 'pointer'
                    }}
                >
                    Go Back
                </button>
            </div>
        );
    }

    return <>{children}</>;
}
