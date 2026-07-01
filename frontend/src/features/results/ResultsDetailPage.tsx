import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  Box, Typography, Card, CardContent, Button, Accordion, AccordionSummary, AccordionDetails,
  Chip, Alert, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper,
} from '@mui/material';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import { BarChart } from '@mui/x-charts/BarChart';
import { RadarChart } from '@mui/x-charts/RadarChart';
import { api } from '../../api/client';
import { useAuth } from '../../auth/AuthContext';
import type { Results, ResultsCategoryGroup } from '../../types';
import {
  RATING_MAX, SERIES_COLORS, buildBarChartDataset, formatAverage, summariesForRadar,
} from './resultsChartUtils';

function CategoryComments({ group }: { group: ResultsCategoryGroup }) {
  const hasComments = group.commentGroups.length > 0 || group.openTextGroups.length > 0;
  if (!hasComments) return null;

  return (
    <Box sx={{ mt: 3 }}>
      <Typography variant="subtitle1" sx={{ mb: 2, fontWeight: 600 }}>Comments</Typography>
      {['Self', 'Peer', 'Manager'].map((type) => {
        const commentGroups = group.commentGroups.filter((g) => g.reviewerType === type);
        const openGroups = group.openTextGroups.filter((g) => g.reviewerType === type);
        if (commentGroups.length === 0 && openGroups.length === 0) return null;
        return (
          <Accordion key={type} defaultExpanded={type === 'Peer'}>
            <AccordionSummary expandIcon={<ExpandMoreIcon />}>
              <Typography sx={{ fontWeight: 600 }}>{type} Feedback</Typography>
            </AccordionSummary>
            <AccordionDetails>
              {commentGroups.map((g, i) => (
                <Box key={i} sx={{ mb: 2 }}>
                  <Typography variant="subtitle2" color="text.secondary">{g.questionText}</Typography>
                  {g.comments.map((c, j) => (
                    <Typography key={j} variant="body2" sx={{ ml: 2, mb: 1 }}>• {c}</Typography>
                  ))}
                </Box>
              ))}
              {openGroups.map((g, i) => (
                <Box key={`o${i}`} sx={{ mb: 2 }}>
                  <Typography variant="subtitle2" color="text.secondary">{g.questionText}</Typography>
                  {g.responses.map((r, j) => (
                    <Typography key={j} variant="body2" sx={{ ml: 2, mb: 1 }}>• {r}</Typography>
                  ))}
                </Box>
              ))}
            </AccordionDetails>
          </Accordion>
        );
      })}
    </Box>
  );
}

export function ResultsDetailPage() {
  const { surveyId } = useParams();
  const { user } = useAuth();
  const [results, setResults] = useState<Results | null>(null);
  const [published, setPublished] = useState(false);

  const load = () => api.get<Results>(`/results/${surveyId}`).then((r) => {
    setResults(r.data);
    setPublished(r.data.resultsPublished);
  });

  useEffect(() => { load(); }, [surveyId]);

  const publish = async () => {
    await api.post(`/surveys/${surveyId}/publish-results`);
    load();
  };

  if (!results) return null;

  const { metrics, series: radarSeries, rated } = summariesForRadar(results.categorySummaries);

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="h5">{results.title}</Typography>
          <Typography variant="body2" color="text.secondary">{results.subjectEmployeeName}</Typography>
        </Box>
        {user?.role === 'Manager' && !published && (
          <Button variant="contained" onClick={publish}>Share Results with Employee</Button>
        )}
        {published && <Chip label="Published to Employee" color="success" />}
      </Box>

      {user?.role === 'Employee' && !published && (
        <Alert severity="info" sx={{ mb: 2 }}>Results have not been published by your manager yet.</Alert>
      )}

      {metrics.length > 0 && (
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Typography variant="h6" sx={{ mb: 2 }}>Category Overview</Typography>
            <RadarChart
              height={400}
              shape="circular"
              radar={{ max: RATING_MAX, metrics }}
              series={radarSeries.map((s) => ({
                label: s.label,
                data: s.data,
                color: s.color,
                fillArea: true,
                valueFormatter: (value: number | null, context: { dataIndex: number }) => {
                  if (value == null || s.nullMask[context.dataIndex]) return 'N/A';
                  return value.toFixed(1);
                },
              }))}
              margin={{ top: 40, bottom: 40 }}
            />

            <TableContainer component={Paper} variant="outlined" sx={{ mt: 3 }}>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Category</TableCell>
                    <TableCell align="right" sx={{ color: SERIES_COLORS.Self }}>Self</TableCell>
                    <TableCell align="right" sx={{ color: SERIES_COLORS.Peer }}>Peer</TableCell>
                    <TableCell align="right" sx={{ color: SERIES_COLORS.Manager }}>Manager</TableCell>
                    <TableCell align="right" sx={{ color: SERIES_COLORS['360'] }}>360</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {rated.map((row) => (
                    <TableRow key={row.categoryId}>
                      <TableCell component="th" scope="row">{row.categoryName}</TableCell>
                      <TableCell align="right">{formatAverage(row.selfAverage)}</TableCell>
                      <TableCell align="right">{formatAverage(row.peerAverage)}</TableCell>
                      <TableCell align="right">{formatAverage(row.managerAverage)}</TableCell>
                      <TableCell align="right">{formatAverage(row.overallAverage)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </CardContent>
        </Card>
      )}

      {results.categoryGroups.map((group) => {
        const chartData = buildBarChartDataset(group);
        return (
          <Card key={group.categoryId} sx={{ mb: 3 }}>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2 }}>{group.categoryName}</Typography>
              {group.labels.length > 0 && (
                <BarChart
                  dataset={chartData}
                  xAxis={[{ scaleType: 'band', dataKey: 'question' }]}
                  series={[
                    { dataKey: 'Self', label: 'Self', color: SERIES_COLORS.Self },
                    { dataKey: 'Peer', label: 'Peer', color: SERIES_COLORS.Peer },
                    { dataKey: 'Manager', label: 'Manager', color: SERIES_COLORS.Manager },
                  ]}
                  height={Math.max(250, group.labels.length * 50)}
                  margin={{ left: 60, bottom: 80 }}
                />
              )}
              <CategoryComments group={group} />
            </CardContent>
          </Card>
        );
      })}
    </Box>
  );
}
