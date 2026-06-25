import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  Box, Typography, Card, CardContent, Button, Accordion, AccordionSummary, AccordionDetails, Chip, Alert,
} from '@mui/material';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import { BarChart } from '@mui/x-charts/BarChart';
import { api } from '../../api/client';
import { useAuth } from '../../auth/AuthContext';
import type { Results } from '../../types';

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

  const chartData = results.labels.map((label, i) => ({
    question: label.length > 30 ? label.slice(0, 30) + '…' : label,
    Self: results.series.find((s) => s.name === 'Self')?.data[i] ?? 0,
    Peer: results.series.find((s) => s.name === 'Peer')?.data[i] ?? 0,
    Manager: results.series.find((s) => s.name === 'Manager')?.data[i] ?? 0,
  }));

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

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" sx={{ mb: 2 }}>360 Ratings Overview</Typography>
          <BarChart
            dataset={chartData}
            xAxis={[{ scaleType: 'band', dataKey: 'question' }]}
            series={[
              { dataKey: 'Self', label: 'Self', color: '#0875E1' },
              { dataKey: 'Peer', label: 'Peer', color: '#5E6A75' },
              { dataKey: 'Manager', label: 'Manager', color: '#2E7D32' },
            ]}
            height={350}
            margin={{ left: 60, bottom: 80 }}
          />
        </CardContent>
      </Card>

      <Typography variant="h6" sx={{ mb: 2 }}>Comments by Reviewer Type</Typography>
      {['Self', 'Peer', 'Manager'].map((type) => {
        const groups = results.commentGroups.filter((g) => g.reviewerType === type);
        const openGroups = results.openTextGroups.filter((g) => g.reviewerType === type);
        if (groups.length === 0 && openGroups.length === 0) return null;
        return (
          <Accordion key={type} defaultExpanded={type === 'Peer'}>
            <AccordionSummary expandIcon={<ExpandMoreIcon />}>
              <Typography sx={{ fontWeight: 600 }}>{type} Feedback</Typography>
            </AccordionSummary>
            <AccordionDetails>
              {groups.map((g, i) => (
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
