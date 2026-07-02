import { useEffect, useState } from 'react';
import {
  Box, Typography, Button, Dialog, DialogTitle, DialogContent, DialogActions,
  TextField, MenuItem, Alert,
} from '@mui/material';
import { DataGrid, GridActionsCellItem } from '@mui/x-data-grid';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import { api } from '../../api/client';
import type { User, UserRole } from '../../types';

const roles: UserRole[] = ['Admin', 'Manager', 'Employee'];

export function UsersPage() {
  const [users, setUsers] = useState<User[]>([]);
  const [managers, setManagers] = useState<User[]>([]);
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<User | null>(null);
  const [deleteConfirm, setDeleteConfirm] = useState<User | null>(null);
  const [form, setForm] = useState({ email: '', name: '', role: 'Employee' as UserRole, managerId: '', password: '' });
  const [error, setError] = useState('');

  const load = () => {
    api.get<User[]>('/admin/users').then((r) => {
      setUsers(r.data);
      setManagers(r.data.filter((u) => u.role === 'Manager'));
    });
  };

  useEffect(() => { load(); }, []);

  const openCreate = () => {
    setEditing(null);
    setForm({ email: '', name: '', role: 'Employee', managerId: '', password: '' });
    setOpen(true);
    setError('');
  };

  const openEdit = (user: User) => {
    setEditing(user);
    setForm({ email: user.email, name: user.name, role: user.role, managerId: user.managerId || '', password: '' });
    setOpen(true);
    setError('');
  };

  const save = async () => {
    try {
      const payload = {
        email: form.email,
        name: form.name,
        role: form.role,
        managerId: form.role === 'Admin' ? null : form.managerId || null,
        password: form.password || undefined,
      };
      if (editing) {
        await api.put(`/admin/users/${editing.id}`, payload);
      } else {
        await api.post('/admin/users', payload);
      }
      setOpen(false);
      load();
    } catch (e: unknown) {
      const msg = (e as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg || 'Failed to save user.');
    }
  };

  const handleDelete = async () => {
    if (!deleteConfirm) return;
    try {
      await api.delete(`/admin/users/${deleteConfirm.id}`);
      setDeleteConfirm(null);
      load();
    } catch (e: unknown) {
      const msg = (e as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg || 'Failed to delete user.');
      setDeleteConfirm(null);
    }
  };

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3 }}>
        <Typography variant="h5">User Management</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>Add User</Button>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError('')}>{error}</Alert>}

      <DataGrid
        autoHeight
        rows={users}
        getRowId={(r) => r.id}
        columns={[
          { field: 'name', headerName: 'Name', flex: 1 },
          { field: 'email', headerName: 'Email', flex: 1 },
          { field: 'role', headerName: 'Role', width: 120 },
          { field: 'managerName', headerName: 'Manager', flex: 1 },
          {
            field: 'isActive', headerName: 'Status', width: 100,
            renderCell: (p) => p.value ? 'Active' : 'Inactive',
          },
          {
            field: 'actions', type: 'actions', width: 100,
            getActions: (p) => [
              <GridActionsCellItem icon={<EditIcon />} label="Edit" onClick={() => openEdit(p.row)} />,
              <GridActionsCellItem icon={<DeleteIcon />} label="Delete" onClick={() => setDeleteConfirm(p.row)} />,
            ],
          },
        ]}
        disableRowSelectionOnClick
      />

      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>{editing ? 'Edit User' : 'Create User'}</DialogTitle>
        <DialogContent>
          <TextField fullWidth label="Name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} sx={{ mt: 1, mb: 2 }} />
          <TextField fullWidth label="Email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} sx={{ mb: 2 }} />
          <TextField select fullWidth label="Role" value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value as UserRole })} sx={{ mb: 2 }}>
            {roles.map((r) => <MenuItem key={r} value={r}>{r}</MenuItem>)}
          </TextField>
          {form.role !== 'Admin' && (
            <TextField select fullWidth label="Manager" value={form.managerId} onChange={(e) => setForm({ ...form, managerId: e.target.value })} sx={{ mb: 2 }}>
              <MenuItem value="">None</MenuItem>
              {managers.map((m) => <MenuItem key={m.id} value={m.id}>{m.name}</MenuItem>)}
            </TextField>
          )}
          <TextField fullWidth label={editing ? 'New Password (optional)' : 'Password'} type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>Cancel</Button>
          <Button variant="contained" onClick={save}>Save</Button>
        </DialogActions>
      </Dialog>

      <Dialog open={!!deleteConfirm} onClose={() => setDeleteConfirm(null)}>
        <DialogTitle>Delete User</DialogTitle>
        <DialogContent>
          <Typography>
            Permanently delete <strong>{deleteConfirm?.name}</strong>? All their 360 review data
            (surveys, assignments, and responses) will also be permanently removed. This cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteConfirm(null)}>Cancel</Button>
          <Button color="error" variant="contained" onClick={handleDelete}>Delete</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
