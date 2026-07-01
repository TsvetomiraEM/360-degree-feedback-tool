import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  Box, Typography, Card, CardContent, Button, Accordion, AccordionSummary, AccordionDetails,
  Chip, Alert, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper,
} from '@mui/material';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import { api } from '../../api/client';
import { CategoryRadarChart } from './CategoryRadarChart';
import { QuestionBarChart } from './QuestionBarChart';
import { useAuth } from '../../auth/AuthContext';
import type { Results, ResultsCategoryGroup } from '../../types';
import {
  SERIES_COLORS, buildBarChartDataset, buildHighlightBarDataset, formatAverage, summariesForRadar,
} from './resultsChartUtils';

const HIGHLIGHT_CHART_SERIES = [
  { dataKey: 'Peer', label: 'Peer', color: SERIES_COLORS.Peer },
  { dataKey: 'Manager', label: 'Manager', color: SERIES_COLORS.Manager },
] as const;

function QuestionHighlightCard({
  title,
  questions,
}: {
  title: string;
  questions: Results['topQuestions'];
}) {
  if (questions.length === 0) return null;

  return (
    <Card sx={{ height: '100%' }}>
      <CardContent>
        <Typography variant="h6" sx={{ mb: 0.5 }}>{title}</Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Ranked by average of Peer and Manager scores
        </Typography>
        <QuestionBarChart
          dataset={buildHighlightBarDataset(questions)}
          series={[...HIGHLIGHT_CHART_SERIES]}
        />
      </CardContent>
    </Card>
  );
}

function CategoryComments({ group }: { group: ResultsCategoryGroup }) {
  const hasComments = group.commentGroups.length > 0 || group.openTextGroups.length > 0;
  if (!hasComments) return null;

  return (
    <Box sx={{ height: '100%', minWidth: 0, overflow: 'auto' }}>
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
            <CategoryRadarChart metrics={metrics} series={radarSeries} />

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

      {(results.topQuestions.length > 0 || results.bottomQuestions.length > 0) && (
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' },
            gap: 3,
            mb: 3,
          }}
        >
          <QuestionHighlightCard title="Top 3 Questions" questions={results.topQuestions} />
          <QuestionHighlightCard title="Bottom 3 Questions" questions={results.bottomQuestions} />
        </Box>
      )}

      {results.categoryGroups.map((group) => {
        const chartData = buildBarChartDataset(group);
        return (
          <Card key={group.categoryId} sx={{ mb: 3 }}>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2 }}>{group.categoryName}</Typography>
              <Box
                sx={{
                  display: 'grid',
                  gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' },
                  gap: 3,
                  alignItems: 'start',
                }}
              >
                {group.labels.length > 0 && (
                  <Box sx={{ minWidth: 0 }}>
                    <QuestionBarChart
                      dataset={chartData}
                      series={[
                        { dataKey: 'Self', label: 'Self', color: SERIES_COLORS.Self },
                        { dataKey: 'Peer', label: 'Peer', color: SERIES_COLORS.Peer },
                        { dataKey: 'Manager', label: 'Manager', color: SERIES_COLORS.Manager },
                      ]}
                    />
                  </Box>
                )}
                <CategoryComments group={group} />
              </Box>
            </CardContent>
          </Card>
        );
      })}
    </Box>
  );
}
