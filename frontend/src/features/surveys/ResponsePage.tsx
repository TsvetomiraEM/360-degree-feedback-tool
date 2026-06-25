import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Box, Typography, Card, CardContent, Button, Rating, TextField, Alert, Chip,
} from '@mui/material';
import { api } from '../../api/client';
import type { AssignmentDetail, ResponseInput } from '../../types';

export function ResponsePage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [assignment, setAssignment] = useState<AssignmentDetail | null>(null);
  const [responses, setResponses] = useState<Record<string, ResponseInput>>({});
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);

  useEffect(() => {
    api.get<AssignmentDetail>(`/assignments/${id}`).then((r) => {
      setAssignment(r.data);
      const init: Record<string, ResponseInput> = {};
      r.data.questions.forEach((q) => {
        const existing = r.data.existingResponses?.find((er) => er.questionId === q.id);
        init[q.id] = existing || { questionId: q.id };
      });
      setResponses(init);
    });
  }, [id]);

  const submit = async () => {
    setError('');
    try {
      await api.post(`/assignments/${id}/submit`, {
        responses: Object.values(responses),
      });
      setSuccess(true);
      setTimeout(() => navigate('/assignments'), 2000);
    } catch (e: unknown) {
      const msg = (e as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg || 'Failed to submit responses.');
    }
  };

  if (!assignment) return null;

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 1 }}>{assignment.surveyTitle}</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Review for {assignment.subjectEmployeeName} · {assignment.reviewerType}
      </Typography>
      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
      {success && <Alert severity="success" sx={{ mb: 2 }}>Responses submitted successfully!</Alert>}

      {assignment.questions.map((q) => (
        <Card key={q.id} sx={{ mb: 2 }}>
          <CardContent>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
              <Chip label={q.categoryName} size="small" color="primary" variant="outlined" />
              <Typography variant="subtitle1">{q.text}</Typography>
            </Box>
            {q.type === 'Rating' ? (
              <>
                <Rating
                  value={responses[q.id]?.rating || 0}
                  onChange={(_, v) => setResponses({ ...responses, [q.id]: { ...responses[q.id], questionId: q.id, rating: v || undefined } })}
                  max={5}
                  sx={{ mb: 2 }}
                />
                <TextField
                  fullWidth multiline rows={2} label="Comment (optional)"
                  value={responses[q.id]?.comment || ''}
                  onChange={(e) => setResponses({ ...responses, [q.id]: { ...responses[q.id], questionId: q.id, comment: e.target.value } })}
                />
              </>
            ) : (
              <TextField
                fullWidth multiline rows={3} label="Your response"
                slotProps={{ htmlInput: { maxLength: 300 } }}
                helperText={`${(responses[q.id]?.openText || '').length}/300`}
                value={responses[q.id]?.openText || ''}
                onChange={(e) => setResponses({ ...responses, [q.id]: { ...responses[q.id], questionId: q.id, openText: e.target.value } })}
              />
            )}
          </CardContent>
        </Card>
      ))}

      {assignment.status === 'Pending' && (
        <Button variant="contained" size="large" onClick={submit}>Submit Responses</Button>
      )}
    </Box>
  );
}
