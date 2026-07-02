import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Box, Typography, Button, Card, CardContent, Chip } from '@mui/material';
import { DataGrid } from '@mui/x-data-grid';
import { api } from '../../api/client';
import { useAuth } from '../../auth/AuthContext';
import type { Survey } from '../../types';

export function ResultsListPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [surveys, setSurveys] = useState<Survey[]>([]);

  useEffect(() => {
    api.get<Survey[]>('/results').then((r) => setSurveys(r.data));
  }, []);

  const title = user?.role === 'Employee' ? 'My 360 Results' : 'Team Results';

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 3 }}>{title}</Typography>
      <DataGrid
        autoHeight rows={surveys} getRowId={(r) => r.id}
        columns={[
          { field: 'title', headerName: 'Survey', flex: 1 },
          { field: 'subjectEmployeeName', headerName: 'Employee', flex: 1 },
          { field: 'status', headerName: 'Status', width: 120 },
          {
            field: 'resultsPublished', headerName: 'Published', width: 120,
            renderCell: (p) => <Chip label={p.value ? 'Yes' : 'No'} size="small" color={p.value ? 'success' : 'default'} />,
          },
          {
            field: 'completedCount', headerName: 'Progress', width: 120,
            valueGetter: (_, r) => `${r.completedCount}/${r.assignmentCount}`,
          },
          {
            field: 'actions', headerName: '', width: 120,
            renderCell: (p) => (
              <Button size="small" onClick={() => navigate(`/results/${p.row.id}`)}>View</Button>
            ),
          },
        ]}
        disableRowSelectionOnClick
      />
      {surveys.length === 0 && (
        <Card sx={{ mt: 2 }}><CardContent><Typography color="text.secondary">No results available yet.</Typography></CardContent></Card>
      )}
    </Box>
  );
}
