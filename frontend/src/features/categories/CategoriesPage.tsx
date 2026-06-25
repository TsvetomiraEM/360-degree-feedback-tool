import { useEffect, useState } from 'react';
import {
  Box, Typography, Button, Dialog, DialogTitle, DialogContent, DialogActions, TextField, Alert,
} from '@mui/material';
import { DataGrid } from '@mui/x-data-grid';
import AddIcon from '@mui/icons-material/Add';
import { api } from '../../api/client';
import type { QuestionCategory } from '../../types';

export function CategoriesPage() {
  const [categories, setCategories] = useState<QuestionCategory[]>([]);
  const [open, setOpen] = useState(false);
  const [name, setName] = useState('');
  const [message, setMessage] = useState('');

  const load = () => api.get<QuestionCategory[]>('/categories').then((r) => setCategories(r.data));

  useEffect(() => { load(); }, []);

  const create = async () => {
    try {
      await api.post('/categories', { name: name.trim() });
      setName('');
      setOpen(false);
      setMessage('');
      load();
    } catch {
      setMessage('Failed to create category.');
    }
  };

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3 }}>
        <Box>
          <Typography variant="h5">Question Categories</Typography>
          <Typography variant="body2" color="text.secondary">
            Custom categories are shared across all managers and can be reused on any question.
          </Typography>
        </Box>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setOpen(true)}>
          New Category
        </Button>
      </Box>

      {message && <Alert severity="error" sx={{ mb: 2 }}>{message}</Alert>}

      <DataGrid
        autoHeight
        rows={categories}
        getRowId={(r) => r.id}
        columns={[
          { field: 'name', headerName: 'Category', flex: 1 },
          { field: 'createdByName', headerName: 'Created By', width: 180 },
          {
            field: 'createdAt', headerName: 'Created', width: 180,
            valueFormatter: (v) => new Date(v as string).toLocaleDateString(),
          },
        ]}
        disableRowSelectionOnClick
      />

      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle>New Category</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus fullWidth label="Name" placeholder="e.g. Skills, Performance, Collaboration"
            value={name} onChange={(e) => setName(e.target.value)} sx={{ mt: 1 }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>Cancel</Button>
          <Button variant="contained" onClick={create} disabled={!name.trim()}>Create</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
