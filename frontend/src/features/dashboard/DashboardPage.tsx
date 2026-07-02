import { useEffect, useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box, Typography, Card, CardContent, Button, Dialog, DialogTitle, DialogContent, DialogActions,
} from '@mui/material';
import { DataGrid, GridActionsCellItem, type GridColDef } from '@mui/x-data-grid';
import AddIcon from '@mui/icons-material/Add';
import DeleteIcon from '@mui/icons-material/Delete';
import BarChartIcon from '@mui/icons-material/BarChart';
import { api } from '../../api/client';
import { useAuth } from '../../auth/AuthContext';
import type { Assignment, Survey } from '../../types';

export function DashboardPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [assignments, setAssignments] = useState<Assignment[]>([]);
  const [surveys, setSurveys] = useState<Survey[]>([]);
  const [deleteTarget, setDeleteTarget] = useState<Survey | null>(null);

  const loadSurveys = useCallback(() => {
    api.get<Survey[]>('/surveys').then((r) => setSurveys(r.data));
  }, []);

  useEffect(() => {
    if (user?.role === 'Manager') {
      loadSurveys();
    }
    if (user?.role === 'Manager' || user?.role === 'Employee') {
      api.get<Assignment[]>('/assignments/mine').then((r) => setAssignments(r.data));
    }
  }, [user, loadSurveys]);

  const confirmDelete = async () => {
    if (!deleteTarget) return;
    await api.delete(`/surveys/${deleteTarget.id}`);
    setDeleteTarget(null);
    loadSurveys();
  };

  const pending = assignments.filter((a) => a.status === 'Pending');

  if (user?.role === 'Manager') {
    return (
      <Box>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3 }}>
          <Typography variant="h5">Team Dashboard</Typography>
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => navigate('/surveys/new')}>
            New 360 Review
          </Button>
        </Box>
        <Box sx={{ display: 'flex', gap: 2, mb: 3, flexWrap: 'wrap' }}>
          {[
            { label: 'Active Surveys', value: surveys.filter((s) => s.status === 'Active').length },
            { label: 'My Pending Reviews', value: pending.length },
            { label: 'Total Surveys', value: surveys.length },
          ].map((stat) => (
            <Card key={stat.label} sx={{ flex: '1 1 200px' }}>
              <CardContent>
                <Typography color="text.secondary" variant="body2">{stat.label}</Typography>
                <Typography variant="h4">{stat.value}</Typography>
              </CardContent>
            </Card>
          ))}
        </Box>
        <Typography variant="h6" sx={{ mb: 2 }}>Recent Surveys</Typography>
        <DataGrid
          autoHeight
          rows={surveys}
          columns={[
            { field: 'title', headerName: 'Title', flex: 1 },
            { field: 'subjectEmployeeName', headerName: 'Employee', flex: 1 },
            { field: 'status', headerName: 'Status', width: 120 },
            { field: 'completedCount', headerName: 'Completed', width: 110 },
            { field: 'assignmentCount', headerName: 'Total', width: 90 },
            {
              field: 'actions', type: 'actions', width: 120,
              getActions: (p) => [
                <GridActionsCellItem icon={<BarChartIcon />} label="Results" onClick={() => navigate(`/results/${p.row.id}`)} />,
                <GridActionsCellItem icon={<DeleteIcon />} label="Delete" onClick={() => setDeleteTarget(p.row)} />,
              ],
            },
          ] as GridColDef[]}
          getRowId={(r) => r.id}
          disableRowSelectionOnClick
          pageSizeOptions={[5]}
          initialState={{ pagination: { paginationModel: { pageSize: 5 } } }}
        />
        <Dialog open={!!deleteTarget} onClose={() => setDeleteTarget(null)}>
          <DialogTitle>Delete 360 Review?</DialogTitle>
          <DialogContent>
            <Typography>
              This will permanently delete &quot;{deleteTarget?.title}&quot; for {deleteTarget?.subjectEmployeeName}, including all assignments and responses.
            </Typography>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setDeleteTarget(null)}>Cancel</Button>
            <Button variant="contained" color="error" onClick={confirmDelete}>Delete</Button>
          </DialogActions>
        </Dialog>
      </Box>
    );
  }

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 3 }}>My Assignments</Typography>
      {pending.length === 0 ? (
        <Card><CardContent><Typography color="text.secondary">No pending surveys. You're all caught up!</Typography></CardContent></Card>
      ) : (
        pending.map((a) => (
          <Card key={a.id} sx={{ mb: 2 }}>
            <CardContent sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <Box>
                <Typography variant="subtitle1">{a.surveyTitle}</Typography>
                <Typography variant="body2" color="text.secondary">
                  Review for {a.subjectEmployeeName} · {a.reviewerType}
                </Typography>
              </Box>
              <Button variant="contained" onClick={() => navigate(`/assignments/${a.id}`)}>Respond</Button>
            </CardContent>
          </Card>
        ))
      )}
    </Box>
  );
}
