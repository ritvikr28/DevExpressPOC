import { useState, FormEvent } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { login, isAuthenticated } from '../../services/authService';
import './Login.css';

interface LocationState {
    from?: { pathname: string };
}

export default function Login() {
    const navigate = useNavigate();
    const location = useLocation();
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState<string | null>(null);
    const [loading, setLoading] = useState(false);

    // Extract the redirect path from location state
    const getRedirectPath = (): string => {
        const state = location.state as LocationState | null;
        return state?.from?.pathname || '/';
    };

    // Redirect if already authenticated
    if (isAuthenticated()) {
        navigate(getRedirectPath(), { replace: true });
        return null;
    }

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setError(null);
        setLoading(true);

        try {
            await login({ username, password });
            navigate(getRedirectPath(), { replace: true });
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Login failed');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="login-container">
            <div className="login-card">
                <h1 className="login-title">DevExpress Reports</h1>
                <h2 className="login-subtitle">Sign In</h2>
                
                {error && (
                    <div className="login-error">
                        {error}
                    </div>
                )}

                <form onSubmit={handleSubmit} className="login-form">
                    <div className="form-group">
                        <label htmlFor="username">Username</label>
                        <input
                            type="text"
                            id="username"
                            value={username}
                            onChange={(e) => setUsername(e.target.value)}
                            placeholder="Enter your username"
                            required
                            disabled={loading}
                            autoComplete="username"
                        />
                    </div>

                    <div className="form-group">
                        <label htmlFor="password">Password</label>
                        <input
                            type="password"
                            id="password"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            placeholder="Enter your password"
                            required
                            disabled={loading}
                            autoComplete="current-password"
                        />
                    </div>

                    <button 
                        type="submit" 
                        className="login-button"
                        disabled={loading}
                    >
                        {loading ? 'Signing in...' : 'Sign In'}
                    </button>
                </form>

                <div className="login-demo-info">
                    <h4>Demo Credentials:</h4>
                    <ul>
                        <li><strong>admin</strong> / admin123 - Full access</li>
                        <li><strong>reporteditor</strong> / editor123 - View &amp; Edit</li>
                        <li><strong>reportviewer</strong> / viewer123 - View only</li>
                    </ul>
                </div>
            </div>
        </div>
    );
}
