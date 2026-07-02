import { useEffect, useState } from 'react';
import {
  Box, TextField, MenuItem, Button, Dialog, DialogTitle, DialogContent, DialogActions, IconButton,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import { api } from '../api/client';
import type { QuestionCategory } from '../types';

interface CategorySelectProps {
  value: string;
  onChange: (categoryId: string) => void;
  sx?: object;
}

export function CategorySelect({ value, onChange, sx }: CategorySelectProps) {
  const [categories, setCategories] = useState<QuestionCategory[]>([]);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [newName, setNewName] = useState('');

  const load = () => api.get<QuestionCategory[]>('/categories').then((r) => setCategories(r.data));

  useEffect(() => { load(); }, []);

  useEffect(() => {
    if (!value && categories.length > 0) {
      onChange(categories[0].id);
    }
  }, [categories, value, onChange]);

  const createCategory = async () => {
    if (!newName.trim()) return;
    const { data } = await api.post<QuestionCategory>('/categories', { name: newName.trim() });
    await load();
    onChange(data.id);
    setNewName('');
    setDialogOpen(false);
  };

  return (
    <>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, ...sx }}>
        <TextField
          select
          label="Category"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          sx={{ minWidth: 150, flex: 1 }}
        >
          {categories.map((c) => (
            <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>
          ))}
        </TextField>
        <IconButton size="small" onClick={() => setDialogOpen(true)} title="Create category">
          <AddIcon />
        </IconButton>
      </Box>

      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle>Create Category</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus fullWidth label="Category name" placeholder="e.g. Skills, Performance"
            value={newName} onChange={(e) => setNewName(e.target.value)} sx={{ mt: 1 }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogOpen(false)}>Cancel</Button>
          <Button variant="contained" onClick={createCategory}>Create</Button>
        </DialogActions>
      </Dialog>
    </>
  );
}

export function useCategories() {
  const [categories, setCategories] = useState<QuestionCategory[]>([]);
  useEffect(() => {
    api.get<QuestionCategory[]>('/categories').then((r) => setCategories(r.data));
  }, []);
  return categories;
}
