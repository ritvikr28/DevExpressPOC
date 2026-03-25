import { createBrowserRouter, createRoutesFromElements, Route } from "react-router-dom";
import Home  from './components/Home';
import ReportViewer from './components/ReportViewer';
import ReportDesigner from './components/ReportDesigner';
import CustomReportDesigner from './components/CustomReportDesigner';
import ReportManagement from './components/ReportManagement';
import Login from './components/Login';
import ProtectedRoute from './components/ProtectedRoute';
import { App } from './App';
import { AppRoles } from './services/authService';

function getBaseUrl() {
    return document.getElementsByTagName('base')[0].href;
}

export const router = createBrowserRouter(createRoutesFromElements(
    <Route path="/" element={<App />}>
        <Route path="login" element={<Login />} />
        <Route path="/" element={
            <ProtectedRoute requiredRoles={[AppRoles.ReportViewer, AppRoles.ReportEditor, AppRoles.Admin]}>
                <Home />
            </ProtectedRoute>
        } />
        <Route path="DocumentViewer" element={
            <ProtectedRoute requiredRoles={[AppRoles.ReportViewer, AppRoles.ReportEditor, AppRoles.Admin]}>
                <ReportViewer hostUrl={getBaseUrl()} />
            </ProtectedRoute>
        } />
        <Route path="ReportDesigner" element={
            <ProtectedRoute requiredRoles={[AppRoles.ReportEditor, AppRoles.Admin]}>
                <ReportDesigner hostUrl={getBaseUrl()} />
            </ProtectedRoute>
        } />
        <Route path="CustomReportDesigner" element={
            <ProtectedRoute requiredRoles={[AppRoles.ReportEditor, AppRoles.Admin]}>
                <CustomReportDesigner hostUrl={getBaseUrl()} />
            </ProtectedRoute>
        } />
        <Route path="ReportManagement" element={
            <ProtectedRoute requiredRoles={[AppRoles.ReportViewer, AppRoles.ReportEditor, AppRoles.Admin]}>
                <ReportManagement />
            </ProtectedRoute>
        } />
    </Route>
));