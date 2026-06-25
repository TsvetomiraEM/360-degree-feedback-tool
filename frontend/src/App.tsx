import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { ThemeProvider, CssBaseline } from '@mui/material';
import { theme } from './theme/theme';
import { AuthProvider, useAuth } from './auth/AuthContext';
import { AppShell } from './layouts/AppShell';
import { RequireAuth, AdminOnly, ManagerOnly, ManagerOrEmployee, ResultsViewer, EmployeeOnly } from './routes/guards';
import { LoginPage } from './features/login/LoginPage';
import { DashboardPage } from './features/dashboard/DashboardPage';
import { UsersPage } from './features/admin/UsersPage';
import { AuditLogsPage } from './features/admin/AuditLogsPage';
import { TemplatesPage } from './features/templates/TemplatesPage';
import { CategoriesPage } from './features/categories/CategoriesPage';
import { SurveyWizardPage } from './features/surveys/SurveyWizardPage';
import { AssignmentsPage } from './features/surveys/AssignmentsPage';
import { ResponsePage } from './features/surveys/ResponsePage';
import { ResultsListPage } from './features/results/ResultsListPage';
import { ResultsDetailPage } from './features/results/ResultsDetailPage';

function HomeRedirect() {
  const { user } = useAuth();
  if (user?.role === 'Admin') return <Navigate to="/admin/users" replace />;
  return <DashboardPage />;
}

export default function App() {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route element={<RequireAuth />}>
              <Route element={<AppShell />}>
                <Route path="/" element={<HomeRedirect />} />

                <Route element={<AdminOnly />}>
                  <Route path="/admin/users" element={<UsersPage />} />
                  <Route path="/admin/audit-logs" element={<AuditLogsPage />} />
                </Route>

                <Route element={<ManagerOnly />}>
                  <Route path="/templates" element={<TemplatesPage />} />
                  <Route path="/categories" element={<CategoriesPage />} />
                  <Route path="/surveys/new" element={<SurveyWizardPage />} />
                  <Route path="/results" element={<ResultsListPage />} />
                </Route>

                <Route element={<ManagerOrEmployee />}>
                  <Route path="/assignments" element={<AssignmentsPage />} />
                  <Route path="/assignments/:id" element={<ResponsePage />} />
                </Route>

                <Route element={<ResultsViewer />}>
                  <Route path="/results/:surveyId" element={<ResultsDetailPage />} />
                </Route>
                <Route element={<EmployeeOnly />}>
                  <Route path="/my-results" element={<ResultsListPage />} />
                </Route>
              </Route>
            </Route>
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </ThemeProvider>
  );
}
