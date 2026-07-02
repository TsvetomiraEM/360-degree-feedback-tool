import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import type { UserRole } from '../types';

export function RequireAuth() {
  const { user, isLoading } = useAuth();
  if (isLoading) return null;
  if (!user) return <Navigate to="/login" replace />;
  return <Outlet />;
}

export function RequireRole({ roles }: { roles: UserRole[] }) {
  const { user } = useAuth();
  if (!user || !roles.includes(user.role)) {
    if (user?.role === 'Admin') return <Navigate to="/admin/users" replace />;
    return <Navigate to="/" replace />;
  }
  return <Outlet />;
}

export function AdminOnly() {
  return <RequireRole roles={['Admin']} />;
}

export function ManagerOnly() {
  return <RequireRole roles={['Manager']} />;
}

export function EmployeeOnly() {
  return <RequireRole roles={['Employee']} />;
}

export function ManagerOrEmployee() {
  return <RequireRole roles={['Manager', 'Employee']} />;
}

export function ResultsViewer() {
  const { user } = useAuth();
  if (!user || user.role === 'Admin') {
    return <Navigate to={user?.role === 'Admin' ? '/admin/users' : '/login'} replace />;
  }
  return <Outlet />;
}
