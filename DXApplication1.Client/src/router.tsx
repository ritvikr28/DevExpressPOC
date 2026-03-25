import { createBrowserRouter, createRoutesFromElements, Route } from "react-router-dom";
import Home  from './components/Home';
import ReportViewer from './components/ReportViewer';
import ReportDesigner from './components/ReportDesigner';
import CustomReportDesigner from './components/CustomReportDesigner';
import ReportManagement from './components/ReportManagement';
import { App } from './App';

function getBaseUrl() {
    return document.getElementsByTagName('base')[0].href;
}

export const router = createBrowserRouter(createRoutesFromElements(
    <Route path="/" element={<App />}>
        <Route path="/" element={<Home />} />
        <Route path="DocumentViewer" element={<ReportViewer hostUrl={getBaseUrl()} />} />
        <Route path="ReportDesigner" element={<ReportDesigner hostUrl={getBaseUrl()} />} />
        <Route path="CustomReportDesigner" element={<CustomReportDesigner hostUrl={getBaseUrl()} />} />
        <Route path="ReportManagement" element={<ReportManagement />} />
    </Route>
));