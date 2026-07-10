import { BrowserRouter, Routes, Route, Link } from 'react-router-dom';
import { ProfilePage } from './pages/ProfilePage';
import { JobListPage } from './pages/JobListPage';
import { JobDetailPage } from './pages/JobDetailPage';
import { ToastProvider } from './components/Toast';
import './App.css';

function App() {
  return (
    <ToastProvider>
    <BrowserRouter>
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
          <Route path="/profile" element={<ProfilePage />} />
          <Route path="*" element={<div style={{ padding: '20px' }}>Page not found.</div>} />
        </Routes>
      </div>
    </BrowserRouter>
    </ToastProvider>
  );
}

export default App;
