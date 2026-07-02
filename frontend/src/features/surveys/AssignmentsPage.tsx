import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Box, Typography, Button, Chip } from '@mui/material';
import { DataGrid } from '@mui/x-data-grid';
import { api } from '../../api/client';
import type { Assignment } from '../../types';

export function AssignmentsPage() {
  const navigate = useNavigate();
  const [assignments, setAssignments] = useState<Assignment[]>([]);

  useEffect(() => {
    api.get<Assignment[]>('/assignments/mine').then((r) => setAssignments(r.data));
  }, []);

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 3 }}>My Surveys</Typography>
      <DataGrid
        autoHeight rows={assignments} getRowId={(r) => r.id}
        columns={[
          { field: 'surveyTitle', headerName: 'Survey', flex: 1 },
          { field: 'subjectEmployeeName', headerName: 'Employee', flex: 1 },
          { field: 'reviewerType', headerName: 'Your Role', width: 120 },
          {
            field: 'status', headerName: 'Status', width: 120,
            renderCell: (p) => <Chip label={p.value} size="small" color={p.value === 'Completed' ? 'success' : 'warning'} />,
          },
          {
            field: 'actions', headerName: '', width: 120,
            renderCell: (p) => p.row.status === 'Pending' ? (
              <Button size="small" variant="contained" onClick={() => navigate(`/assignments/${p.row.id}`)}>Respond</Button>
            ) : null,
          },
        ]}
        disableRowSelectionOnClick
      />
    </Box>
  );
}
