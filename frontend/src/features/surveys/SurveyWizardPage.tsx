import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box, Typography, Button, Stepper, Step, StepLabel, Card, CardContent,
  TextField, MenuItem, Checkbox, FormControlLabel, Alert,
} from '@mui/material';
import { api } from '../../api/client';
import type { Template, User, QuestionInput } from '../../types';
import { CategorySelect, useCategories } from '../../components/CategorySelect';

const steps = ['Select Template', 'Choose Employee', 'Assign Peers', 'Review & Launch'];

export function SurveyWizardPage() {
  const categories = useCategories();
  const defaultCategoryId = categories[0]?.id ?? '';
  const navigate = useNavigate();
  const [activeStep, setActiveStep] = useState(0);
  const [templates, setTemplates] = useState<Template[]>([]);
  const [employees, setEmployees] = useState<User[]>([]);
  const [peers, setPeers] = useState<User[]>([]);
  const [selectedTemplate, setSelectedTemplate] = useState('');
  const [selectedEmployee, setSelectedEmployee] = useState('');
  const [selectedPeers, setSelectedPeers] = useState<string[]>([]);
  const [title, setTitle] = useState('');
  const [dueDate, setDueDate] = useState('');
  const [customQuestions, setCustomQuestions] = useState<QuestionInput[]>([]);
  const [useCustom, setUseCustom] = useState(false);
  const [error, setError] = useState('');
  const [surveyId, setSurveyId] = useState('');

  useEffect(() => {
    api.get<Template[]>('/templates').then((r) => setTemplates(r.data));
    api.get<User[]>('/surveys/direct-reports').then((r) => setEmployees(r.data));
  }, []);

  useEffect(() => {
    if (selectedEmployee) {
      api.get<User[]>(`/surveys/peer-candidates/${selectedEmployee}`).then((r) => setPeers(r.data));
    }
  }, [selectedEmployee]);

  const next = async () => {
    setError('');
    try {
      if (activeStep === 2) {
        let id = surveyId;
        if (!id) {
          if (useCustom) {
            const { data } = await api.post('/surveys', {
              title: title || '360 Review',
              subjectEmployeeId: selectedEmployee,
              dueDate: dueDate || null,
              questions: customQuestions.map((q, i) => ({ ...q, order: i })),
            });
            id = data.id;
          } else {
            const { data } = await api.post(`/surveys/from-template/${selectedTemplate}`, {
              subjectEmployeeId: selectedEmployee,
              dueDate: dueDate || null,
              title: title || undefined,
            });
            id = data.id;
          }
          setSurveyId(id);
        }
        await api.post(`/surveys/${id}/assign`, { peerIds: selectedPeers });
        setActiveStep(3);
      } else {
        setActiveStep((s) => s + 1);
      }
    } catch {
      setError('Failed to proceed. Please check your selections.');
    }
  };

  return (
    <Box>
      <Typography variant="h5" sx={{ mb: 3 }}>Create 360 Review</Typography>
      <Stepper activeStep={activeStep} sx={{ mb: 4 }}>
        {steps.map((label) => <Step key={label}><StepLabel>{label}</StepLabel></Step>)}
      </Stepper>
      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      {activeStep === 0 && (
        <Card><CardContent>
          <FormControlLabel control={<Checkbox checked={useCustom} onChange={(e) => setUseCustom(e.target.checked)} />} label="Create custom survey (no template)" />
          {!useCustom ? (
            <TextField select fullWidth label="Template" value={selectedTemplate} onChange={(e) => setSelectedTemplate(e.target.value)} sx={{ mt: 2 }}>
              {templates.map((t) => <MenuItem key={t.id} value={t.id}>{t.name}</MenuItem>)}
            </TextField>
          ) : (
            <Box sx={{ mt: 2 }}>
              <TextField fullWidth label="Survey Title" value={title} onChange={(e) => setTitle(e.target.value)} sx={{ mb: 2 }} />
              <CategorySelect
                value={customQuestions[0]?.categoryId || defaultCategoryId}
                onChange={(categoryId) => setCustomQuestions([{
                  type: 'Rating',
                  text: customQuestions[0]?.text || '',
                  order: 0,
                  categoryId,
                }])}
                sx={{ mb: 2 }}
              />
              <TextField fullWidth label="Question 1 (Rating)" value={customQuestions[0]?.text || ''} onChange={(e) => setCustomQuestions([{
                type: 'Rating',
                text: e.target.value,
                order: 0,
                categoryId: customQuestions[0]?.categoryId || defaultCategoryId,
              }])} />
            </Box>
          )}
        </CardContent></Card>
      )}

      {activeStep === 1 && (
        <Card><CardContent>
          <TextField select fullWidth label="Employee" value={selectedEmployee} onChange={(e) => setSelectedEmployee(e.target.value)} sx={{ mb: 2 }}>
            {employees.map((e) => <MenuItem key={e.id} value={e.id}>{e.name}</MenuItem>)}
          </TextField>
          <TextField fullWidth type="date" label="Due Date" value={dueDate} onChange={(e) => setDueDate(e.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
        </CardContent></Card>
      )}

      {activeStep === 2 && (
        <Card><CardContent>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Self and manager reviews are added automatically. Select peer reviewers:
          </Typography>
          {peers.map((p) => (
            <FormControlLabel
              key={p.id}
              control={<Checkbox checked={selectedPeers.includes(p.id)} onChange={(e) => {
                setSelectedPeers(e.target.checked ? [...selectedPeers, p.id] : selectedPeers.filter((id) => id !== p.id));
              }} />}
              label={p.name}
            />
          ))}
        </CardContent></Card>
      )}

      {activeStep === 3 && (
        <Card><CardContent>
          <Typography variant="h6" color="success.main">360 Review launched successfully!</Typography>
          <Typography sx={{ mt: 1 }}>Peers, self, and manager have been assigned.</Typography>
          <Button variant="contained" sx={{ mt: 2 }} onClick={() => navigate('/')}>Go to Dashboard</Button>
        </CardContent></Card>
      )}

      {activeStep < 3 && (
        <Box sx={{ mt: 3, display: 'flex', gap: 2 }}>
          <Button disabled={activeStep === 0} onClick={() => setActiveStep((s) => s - 1)}>Back</Button>
          <Button variant="contained" onClick={next} disabled={
            (activeStep === 0 && !useCustom && !selectedTemplate) ||
            (activeStep === 0 && useCustom && (!customQuestions[0]?.text || !customQuestions[0]?.categoryId)) ||
            (activeStep === 1 && !selectedEmployee)
          }>
            {activeStep === 2 ? 'Launch' : 'Next'}
          </Button>
        </Box>
      )}
    </Box>
  );
}
