import { Link, useNavigate } from 'react-router-dom';
import { useState } from 'react';
import { isAuthenticated, getUser, logout, hasAnyRole, AppRoles } from '../../services/authService';

export default function NavMenu() {
    const [isExpanded, setExpanded] = useState(false);
    const navigate = useNavigate();
    const user = getUser();
    const authenticated = isAuthenticated();
    const canEdit = hasAnyRole([AppRoles.ReportEditor, AppRoles.Admin]);

    const toggle = () => {
        setExpanded(!isExpanded);
    }

    const handleLogout = () => {
        logout();
        navigate('/login');
    }

    // Don't show nav menu on login page or when not authenticated
    if (!authenticated) {
        return null;
    }

    return (
        <>
            <header>
                <nav className="navbar navbar-expand-sm navbar-toggleable-sm navbar-light bg-white border-bottom box-shadow">
                    <div className="container-fluid">
                        <Link className="navbar-brand" to="/">ReactReportingApp</Link>
                        <button className="navbar-toggler"
                            type="button"
                            data-toggle="collapse"
                            data-target=".navbar-collapse"
                            aria-label="Toggle navigation"
                            aria-expanded={isExpanded}
                            onClick={toggle} >
                            <span className="navbar-toggler-icon"></span>
                        </button>
                        <div className={`navbar-collapse collapse d-sm-inline-flex flex-sm-row-reverse ${isExpanded && "show"}`}>
                            <ul className="navbar-nav flex-grow">
                                <li className="nav-item" >
                                    <Link className="nav-link text-dark" to="/">Home</Link>
                                </li>
                                {canEdit && (
                                    <>
                                        <li className="nav-item">
                                            <Link className="nav-link text-dark" to="/ReportDesigner">Report Designer</Link>
                                        </li>
                                        <li className="nav-item">
                                            <Link className="nav-link text-dark" to="/CustomReportDesigner">Custom Report Designer</Link>
                                        </li>
                                    </>
                                )}
                                <li className="nav-item">
                                    <Link className="nav-link text-dark" to="/DocumentViewer">Document Viewer</Link>
                                </li>
                                <li className="nav-item">
                                    <Link className="nav-link text-dark" to="/ReportManagement">Report Management</Link>
                                </li>
                            </ul>
                            <div className="navbar-nav ms-auto">
                                <span className="nav-link text-muted">
                                    {user?.username} ({user?.roles?.join(', ')})
                                </span>
                                <button 
                                    className="btn btn-outline-secondary btn-sm"
                                    onClick={handleLogout}
                                >
                                    Logout
                                </button>
                            </div>
                        </div>
                    </div>
                </nav>
            </header>
        </>
    );
}