import { useEffect, useState } from 'react';
import {
  Box, Typography, Button, Dialog, DialogTitle, DialogContent, DialogActions,
  TextField, MenuItem, IconButton, Chip,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import DeleteIcon from '@mui/icons-material/Delete';
import EditIcon from '@mui/icons-material/Edit';
import ShareIcon from '@mui/icons-material/Share';
import { DataGrid, GridActionsCellItem } from '@mui/x-data-grid';
import { api } from '../../api/client';
import type { Template, QuestionInput, User } from '../../types';
import { CategorySelect, useCategories } from '../../components/CategorySelect';

export function TemplatesPage() {
  const categories = useCategories();
  const defaultCategoryId = categories[0]?.id ?? '';
  const [templates, setTemplates] = useState<Template[]>([]);
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<Template | null>(null);
  const [shareOpen, setShareOpen] = useState<Template | null>(null);
  const [managers, setManagers] = useState<User[]>([]);
  const [selectedManagers, setSelectedManagers] = useState<string[]>([]);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [questions, setQuestions] = useState<QuestionInput[]>([
    { type: 'Rating', text: 'Communicates effectively', order: 0, categoryId: '' },
  ]);

  const load = () => api.get<Template[]>('/templates').then((r) => setTemplates(r.data));

  useEffect(() => { load(); }, []);

  const addQuestion = (type: 'Rating' | 'OpenText') => {
    setQuestions([...questions, { type, text: '', order: questions.length, categoryId: defaultCategoryId }]);
  };

  const resetForm = () => {
    setName('');
    setDescription('');
    setQuestions([{ type: 'Rating', text: '', order: 0, categoryId: defaultCategoryId }]);
    setEditing(null);
  };

  const openCreate = () => {
    resetForm();
    setOpen(true);
  };

  const openEdit = (template: Template) => {
    setEditing(template);
    setName(template.name);
    setDescription(template.description ?? '');
    setQuestions(template.questions.map((q, i) => ({ ...q, order: i })));
    setOpen(true);
  };

  const closeDialog = () => {
    setOpen(false);
    resetForm();
  };

  const save = async () => {
    const qs = questions.map((q, i) => ({ ...q, order: i, categoryId: q.categoryId || defaultCategoryId }));
    if (qs.some((q) => !q.categoryId)) return;
    const payload = { name, description, questions: qs };
    if (editing) {
      await api.put(`/templates/${editing.id}`, payload);
    } else {
      await api.post('/templates', payload);
    }
    closeDialog();
    load();
  };

  const remove = async (id: string) => {
    await api.delete(`/templates/${id}`);
    load();
  };

  const openShare = async (t: Template) => {
    const { data } = await api.get<User[]>('/surveys/managers');
    setManagers(data);
    setShareOpen(t);
    setSelectedManagers([]);
  };

  const share = async () => {
    if (!shareOpen) return;
    await api.post(`/templates/${shareOpen.id}/share`, { managerIds: selectedManagers });
    setShareOpen(null);
    load();
  };

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3 }}>
        <Typography variant="h5">Survey Templates</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>New Template</Button>
      </Box>
      <DataGrid
        autoHeight rows={templates} getRowId={(r) => r.id}
        columns={[
          { field: 'name', headerName: 'Name', flex: 1 },
          { field: 'createdByName', headerName: 'Owner', width: 150 },
          { field: 'questions', headerName: 'Questions', width: 110, valueGetter: (_, r) => r.questions.length },
          {
            field: 'tags', headerName: 'Access', width: 150,
            renderCell: (p) => (
              <>
                {p.row.isOwner && <Chip label="Owner" size="small" sx={{ mr: 0.5 }} />}
                {p.row.isShared && <Chip label="Shared" size="small" color="info" />}
              </>
            ),
          },
          {
            field: 'actions', type: 'actions', width: 120,
            getActions: (p) => p.row.isOwner ? [
              <GridActionsCellItem icon={<EditIcon />} label="Edit" onClick={() => openEdit(p.row)} />,
              <GridActionsCellItem icon={<ShareIcon />} label="Share" onClick={() => openShare(p.row)} />,
              <GridActionsCellItem icon={<DeleteIcon />} label="Delete" onClick={() => remove(p.row.id)} />,
            ] : [],
          },
        ]}
        disableRowSelectionOnClick
      />

      <Dialog open={open} onClose={closeDialog} maxWidth="md" fullWidth>
        <DialogTitle>{editing ? 'Edit Template' : 'Create Template'}</DialogTitle>
        <DialogContent>
          <TextField fullWidth label="Name" value={name} onChange={(e) => setName(e.target.value)} sx={{ mt: 1, mb: 2 }} />
          <TextField fullWidth label="Description" value={description} onChange={(e) => setDescription(e.target.value)} sx={{ mb: 2 }} />
          <Typography variant="subtitle2" sx={{ mb: 1 }}>Questions</Typography>
          {questions.map((q, i) => (
            <Box key={i} sx={{ display: 'flex', gap: 1, mb: 1, alignItems: 'flex-start' }}>
              <CategorySelect
                value={q.categoryId || defaultCategoryId}
                onChange={(categoryId) => {
                  const n = [...questions]; n[i].categoryId = categoryId; setQuestions(n);
                }}
              />
              <TextField select label="Type" value={q.type} onChange={(e) => {
                const n = [...questions]; n[i].type = e.target.value as 'Rating' | 'OpenText'; setQuestions(n);
              }} sx={{ width: 140 }}>
                <MenuItem value="Rating">Rating 1-5</MenuItem>
                <MenuItem value="OpenText">Open Text</MenuItem>
              </TextField>
              <TextField fullWidth label="Question" value={q.text} onChange={(e) => {
                const n = [...questions]; n[i].text = e.target.value; setQuestions(n);
              }} slotProps={{ htmlInput: { maxLength: q.type === 'OpenText' ? 300 : undefined } }} />
              <IconButton onClick={() => setQuestions(questions.filter((_, j) => j !== i))}><DeleteIcon /></IconButton>
            </Box>
          ))}
          <Button onClick={() => addQuestion('Rating')} sx={{ mr: 1 }}>+ Rating</Button>
          <Button onClick={() => addQuestion('OpenText')}>+ Open Text</Button>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeDialog}>Cancel</Button>
          <Button variant="contained" onClick={save}>{editing ? 'Save Changes' : 'Save'}</Button>
        </DialogActions>
      </Dialog>

      <Dialog open={!!shareOpen} onClose={() => setShareOpen(null)}>
        <DialogTitle>Share Template</DialogTitle>
        <DialogContent>
          <TextField
            select fullWidth label="Managers"
            value={selectedManagers}
            onChange={(e) => setSelectedManagers(typeof e.target.value === 'string' ? e.target.value.split(',') : e.target.value)}
            sx={{ mt: 1, minWidth: 300 }}
            slotProps={{ select: { multiple: true } }}
          >
            {managers.map((m) => <MenuItem key={m.id} value={m.id}>{m.name}</MenuItem>)}
          </TextField>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShareOpen(null)}>Cancel</Button>
          <Button variant="contained" onClick={share}>Share</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
