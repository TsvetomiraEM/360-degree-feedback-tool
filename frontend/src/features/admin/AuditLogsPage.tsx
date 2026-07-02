import { useEffect, useState } from 'react';
import { Box, Typography, MenuItem, TextField } from '@mui/material';
import { DataGrid } from '@mui/x-data-grid';
import { api } from '../../api/client';
import type { AuditLog, AuditAction, PagedResult } from '../../types';

const actions: AuditAction[] = ['UserCreated', 'UserUpdated', 'UserDeleted', 'UserActivated', 'UserDeactivated'];

export function AuditLogsPage() {
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(0);
  const [action, setAction] = useState<AuditAction | ''>('');

  useEffect(() => {
    const params = new URLSearchParams({ page: String(page + 1), pageSize: '20' });
    if (action) params.set('action', action);
    api.get<PagedResult<AuditLog>>(`/admin/audit-logs?${params}`).then((r) => {
      setLogs(r.data.items);
      setTotal(r.data.totalCount);
    });
  }, [page, action]);

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 3 }}>Audit Logs</Typography>
      <TextField
        select label="Filter by action" value={action} onChange={(e) => { setAction(e.target.value as AuditAction | ''); setPage(0); }}
        sx={{ mb: 2, minWidth: 220 }}
      >
        <MenuItem value="">All actions</MenuItem>
        {actions.map((a) => <MenuItem key={a} value={a}>{a}</MenuItem>)}
      </TextField>
      <DataGrid
        autoHeight
        rows={logs}
        getRowId={(r) => r.id}
        rowCount={total}
        paginationMode="server"
        paginationModel={{ page, pageSize: 20 }}
        onPaginationModelChange={(m) => setPage(m.page)}
        columns={[
          { field: 'createdAt', headerName: 'Date', width: 180, valueFormatter: (v) => new Date(v as string).toLocaleString() },
          { field: 'actorName', headerName: 'Actor', width: 150 },
          { field: 'action', headerName: 'Action', width: 150 },
          { field: 'targetUserName', headerName: 'Target User', width: 150 },
          { field: 'metadata', headerName: 'Details', flex: 1 },
        ]}
        disableRowSelectionOnClick
        pageSizeOptions={[20]}
      />
    </Box>
  );
}
