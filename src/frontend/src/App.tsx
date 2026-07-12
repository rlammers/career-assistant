import { BrowserRouter, Routes, Route, Link, useNavigate } from 'react-router-dom';
import { useCallback, useEffect, useState } from 'react';
import { ProfilePage } from './pages/ProfilePage';
import { JobListPage } from './pages/JobListPage';
import { JobDetailPage } from './pages/JobDetailPage';
import { ToastProvider } from './components/Toast';
import { AuthGate } from './components/AuthGate';
import { authenticationEnabled } from './auth/authConfig';
import { profileAPI, ApiError } from './services/api';
import type { Profile } from './services/api';
import './App.css';

type ProfileGateState = 'loading' | 'missing' | 'ready' | 'error';

export function ProfileGate() {
  const navigate = useNavigate();
  const [state, setState] = useState<ProfileGateState>('loading');
  const [profile, setProfile] = useState<Profile | null>(null);
  const [error, setError] = useState('');

  const loadProfile = useCallback(async () => {
    setState('loading');
    setError('');
    try {
      const currentProfile = await profileAPI.getProfile();
      setProfile(currentProfile);
      setState('ready');
    } catch (profileError) {
      if (profileError instanceof ApiError && profileError.status === 404) {
        setProfile(null);
        setState('missing');
        return;
      }
      setError(profileError instanceof Error ? profileError.message : 'Failed to load your profile.');
      setState('error');
    }
  }, []);

  useEffect(() => {
    void loadProfile();
  }, [loadProfile]);

  const handleProfileSaved = (savedProfile: Profile) => {
    const wasMissing = state === 'missing';
    setProfile(savedProfile);
    setState('ready');
    if (wasMissing) navigate('/jobs');
  };

  if (state === 'loading') {
    return <main className="page-state" aria-busy="true"><p>Checking your profile…</p></main>;
  }

  if (state === 'error') {
    return (
      <main className="page-state">
        <p className="inline-error" role="alert">{error}</p>
        <button type="button" onClick={() => void loadProfile()}>Try again</button>
      </main>
    );
  }

  if (state === 'missing') {
    return (
      <ProfilePage
        initialProfile={null}
        onProfileSaved={handleProfileSaved}
      />
    );
  }

  return <ApplicationContent profile={profile} />;
}

interface ApplicationContentProps {
  profile: Profile | null;
}

function ApplicationContent({ profile }: ApplicationContentProps) {
  return (
    <>
      <div style={{ minHeight: '100vh', backgroundColor: '#fafafa' }}>
        {/* Navigation Header */}
        <nav style={{ backgroundColor: '#333', color: 'white', padding: '15px 20px', marginBottom: '20px' }}>
          <div style={{ maxWidth: '1000px', margin: '0 auto', display: 'flex', gap: '20px', alignItems: 'center' }}>
            <Link
              to="/"
              style={{
                color: 'white',
                textDecoration: 'none',
                fontSize: '20px',
                fontWeight: 'bold',
              }}
            >
              Career Assistant
            </Link>
            <Link
              to="/profile"
              style={{
                color: 'white',
                textDecoration: 'none',
                marginLeft: '20px',
              }}
            >
              Profile
            </Link>
            <Link
              to="/jobs"
              style={{
                color: 'white',
                textDecoration: 'none',
              }}
            >
              Jobs
            </Link>
          </div>
        </nav>

        {/* Routes */}
        <Routes>
          <Route path="/" element={<JobListPage />} />
          <Route path="/jobs" element={<JobListPage />} />
          <Route path="/jobs/:id" element={<JobDetailPage />} />
          <Route path="/profile" element={<ProfilePage initialProfile={profile} />} />
          <Route path="*" element={<div style={{ padding: '20px' }}>Page not found.</div>} />
        </Routes>
      </div>
    </>
  );
}

function App() {
  const application = (
    <BrowserRouter>
      <ToastProvider><ProfileGate /></ToastProvider>
    </BrowserRouter>
  );
  return authenticationEnabled ? <AuthGate>{application}</AuthGate> : application;
}

export default App;
