import { useMemo } from 'react';
import { useNavigate, useLocation, Outlet } from 'react-router-dom';
import {
  Box, Drawer, AppBar, Toolbar, Typography, List, ListItemButton,
  ListItemIcon, ListItemText, IconButton, Menu, MenuItem, Divider,
} from '@mui/material';
import DashboardIcon from '@mui/icons-material/Dashboard';
import AssignmentIcon from '@mui/icons-material/Assignment';
import DescriptionIcon from '@mui/icons-material/Description';
import CategoryIcon from '@mui/icons-material/Category';
import BarChartIcon from '@mui/icons-material/BarChart';
import PeopleIcon from '@mui/icons-material/People';
import HistoryIcon from '@mui/icons-material/History';
import AccountCircleIcon from '@mui/icons-material/AccountCircle';
import { useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import type { UserRole } from '../types';

const DRAWER_WIDTH = 240;

interface NavItem {
  label: string;
  path: string;
  icon: React.ReactNode;
  roles: UserRole[];
}

const navItems: NavItem[] = [
  { label: 'Dashboard', path: '/', icon: <DashboardIcon />, roles: ['Manager', 'Employee'] },
  { label: 'My Surveys', path: '/assignments', icon: <AssignmentIcon />, roles: ['Manager', 'Employee'] },
  { label: 'Templates', path: '/templates', icon: <DescriptionIcon />, roles: ['Manager'] },
  { label: 'Categories', path: '/categories', icon: <CategoryIcon />, roles: ['Manager'] },
  { label: 'Team Results', path: '/results', icon: <BarChartIcon />, roles: ['Manager'] },
  { label: 'My Results', path: '/my-results', icon: <BarChartIcon />, roles: ['Employee'] },
  { label: 'Users', path: '/admin/users', icon: <PeopleIcon />, roles: ['Admin'] },
  { label: 'Audit Logs', path: '/admin/audit-logs', icon: <HistoryIcon />, roles: ['Admin'] },
];

export function AppShell() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

  const items = useMemo(
    () => navItems.filter((item) => user && item.roles.includes(user.role)),
    [user]
  );

  const currentLabel = items.find((i) => i.path === location.pathname)?.label ?? 'Feedback360';

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <AppBar position="fixed" sx={{ zIndex: (t) => t.zIndex.drawer + 1, bgcolor: '#0875E1' }}>
        <Toolbar>
          <Typography variant="h6" sx={{ flexGrow: 1, fontWeight: 700 }}>
            Feedback360
          </Typography>
          <Typography variant="body2" sx={{ mr: 2, opacity: 0.9 }}>
            {currentLabel}
          </Typography>
          <IconButton color="inherit" onClick={(e) => setAnchorEl(e.currentTarget)}>
            <AccountCircleIcon />
          </IconButton>
          <Menu anchorEl={anchorEl} open={Boolean(anchorEl)} onClose={() => setAnchorEl(null)}>
            <MenuItem disabled>{user?.name}</MenuItem>
            <MenuItem disabled sx={{ fontSize: 12 }}>{user?.email}</MenuItem>
            <Divider />
            <MenuItem onClick={() => { setAnchorEl(null); logout(); navigate('/login'); }}>
              Sign out
            </MenuItem>
          </Menu>
        </Toolbar>
      </AppBar>

      <Drawer
        variant="permanent"
        sx={{
          width: DRAWER_WIDTH,
          flexShrink: 0,
          '& .MuiDrawer-paper': { width: DRAWER_WIDTH, boxSizing: 'border-box', borderRight: '1px solid #E5E8EB' },
        }}
      >
        <Toolbar />
        <Box sx={{ overflow: 'auto', pt: 1 }}>
          <List>
            {items.map((item) => (
              <ListItemButton
                key={item.path}
                selected={location.pathname === item.path || location.pathname.startsWith(item.path + '/')}
                onClick={() => navigate(item.path)}
                sx={{ mx: 1, borderRadius: 1, mb: 0.5 }}
              >
                <ListItemIcon sx={{ minWidth: 40 }}>{item.icon}</ListItemIcon>
                <ListItemText primary={item.label} />
              </ListItemButton>
            ))}
          </List>
        </Box>
      </Drawer>

      <Box component="main" sx={{ flexGrow: 1, p: 3, bgcolor: 'background.default' }}>
        <Toolbar />
        <Outlet />
      </Box>
    </Box>
  );
}
