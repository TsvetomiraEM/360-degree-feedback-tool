import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { GoogleOAuthProvider, GoogleLogin } from '@react-oauth/google';
import {
  Box, Card, CardContent, TextField, Button, Typography, Alert, Divider,
} from '@mui/material';
import { useAuth } from '../../auth/AuthContext';

const GOOGLE_CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID || '';
const hasGoogle = GOOGLE_CLIENT_ID && GOOGLE_CLIENT_ID !== 'your-google-client-id';

export function LoginPage() {
  const { login, googleLogin } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');

  const redirectByRole = (role: string) => {
    navigate(role === 'Admin' ? '/admin/users' : '/');
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    try {
      await login(email, password);
      const user = JSON.parse(localStorage.getItem('user') || '{}');
      redirectByRole(user.role);
    } catch {
      setError('Invalid email or password.');
    }
  };

  const handleGoogle = async (credential: string) => {
    setError('');
    try {
      await googleLogin(credential);
      const user = JSON.parse(localStorage.getItem('user') || '{}');
      redirectByRole(user.role);
    } catch {
      setError('Google sign-in failed. Account must be pre-provisioned.');
    }
  };

  const content = (
    <Card sx={{ width: 420, p: 2 }}>
      <CardContent>
        <Typography variant="h5" gutterBottom color="primary">Feedback360</Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
          Sign in to your 360 feedback portal
        </Typography>
        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        <Box component="form" onSubmit={handleSubmit}>
          <TextField fullWidth label="Email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} sx={{ mb: 2 }} required />
          <TextField fullWidth label="Password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} sx={{ mb: 2 }} required />
          <Button type="submit" variant="contained" fullWidth size="large">Sign in</Button>
        </Box>
        {hasGoogle && (
          <>
            <Divider sx={{ my: 3 }}>or</Divider>
            <GoogleLogin onSuccess={(r) => r.credential && handleGoogle(r.credential)} onError={() => setError('Google sign-in failed.')} width="372" />
          </>
        )}
        <Typography variant="caption" color="text.secondary" sx={{ mt: 3, display: 'block' }}>
          Demo: admin@feedback360.local / Admin123! · manager@feedback360.local / Manager123!
        </Typography>
      </CardContent>
    </Card>
  );

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', bgcolor: '#F5F6F7' }}>
      {hasGoogle ? <GoogleOAuthProvider clientId={GOOGLE_CLIENT_ID}>{content}</GoogleOAuthProvider> : content}
    </Box>
  );
}
