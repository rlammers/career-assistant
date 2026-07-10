import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { jobAPI, analysisAPI } from '../services/api';
import type { JobApplication } from '../services/api';

export const JobListPage = () => {
  const navigate = useNavigate();
  const [jobs, setJobs] = useState<JobApplication[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [formData, setFormData] = useState({
    company: '',
    role: '',
    jobDescription: '',
  });
  const [showForm, setShowForm] = useState(false);
  const [analyzingJobId, setAnalyzingJobId] = useState<number | null>(null);
  const [deletingJobId, setDeletingJobId] = useState<number | null>(null);

  useEffect(() => {
    fetchJobs();
  }, []);

  const fetchJobs = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await jobAPI.getJobs();
      setJobs(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch jobs');
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleAddJob = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      const newJob = await jobAPI.createJob(formData);
      setJobs((prev) => [...prev, newJob]);
      setFormData({ company: '', role: '', jobDescription: '' });
      setShowForm(false);
      alert('Job added successfully!');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add job');
    } finally {
      setLoading(false);
    }
  };

  const handleAnalyzeJob = async (jobId: number) => {
    if (analyzingJobId !== null) return;

    setAnalyzingJobId(jobId);
    setError(null);
    try {
      await analysisAPI.analyzeJob(jobId);
      alert('Job analyzed! View the details to see the analysis.');
      await fetchJobs(); // Refresh to show new analysis
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to analyze job');
    } finally {
      setAnalyzingJobId(null);
    }
  };

  const handleViewJob = (jobId: number) => {
    navigate(`/jobs/${jobId}`);
  };

  const handleDeleteJob = async (job: JobApplication) => {
    if (deletingJobId !== null || analyzingJobId !== null) return;
    if (!window.confirm(`Delete ${job.role} at ${job.company}? This will also delete its analysis data.`)) return;

    setDeletingJobId(job.id);
    setError(null);
    try {
      await jobAPI.deleteJob(job.id);
      setJobs((prev) => prev.filter((existingJob) => existingJob.id !== job.id));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete job');
    } finally {
      setDeletingJobId(null);
    }
  };

  return (
    <div style={{ maxWidth: '800px', margin: '0 auto', padding: '20px' }}>
      <h1>Job Applications</h1>
      {error && <div style={{ color: 'red', marginBottom: '10px' }}>{error}</div>}

      <button
        onClick={() => setShowForm(!showForm)}
        style={{ marginBottom: '20px', padding: '10px 20px', cursor: 'pointer' }}
      >
        {showForm ? 'Cancel' : 'Add New Job'}
      </button>

      {showForm && (
        <form onSubmit={handleAddJob} style={{ marginBottom: '20px', padding: '15px', border: '1px solid #ddd' }}>
          <div style={{ marginBottom: '10px' }}>
            <label htmlFor="company">Company</label>
            <input
              className="form-input"
              id="company"
              type="text"
              name="company"
              value={formData.company}
              onChange={handleChange}
              required
            />
          </div>
          <div style={{ marginBottom: '10px' }}>
            <label htmlFor="role">Role</label>
            <input
              className="form-input"
              id="role"
              type="text"
              name="role"
              value={formData.role}
              onChange={handleChange}
              required
            />
          </div>
          <div style={{ marginBottom: '10px' }}>
            <label htmlFor="jobDescription">Job Description</label>
            <textarea
              className="form-textarea form-textarea--large"
              id="jobDescription"
              name="jobDescription"
              value={formData.jobDescription}
              onChange={handleChange}
              required
              rows={6}
            />
          </div>
          <button type="submit" disabled={loading} style={{ padding: '10px 20px', cursor: 'pointer' }}>
            {loading ? 'Adding...' : 'Add Job'}
          </button>
        </form>
      )}

      {loading && <p>Loading jobs...</p>}

      {jobs.length === 0 && !loading && <p>No jobs yet. Add one to get started!</p>}

      <div>
        {jobs.map((job) => (
          <div
            key={job.id}
            style={{
              marginBottom: '15px',
              padding: '15px',
              border: '1px solid #ddd',
              borderRadius: '4px',
            }}
          >
            <h3>{job.role}</h3>
            <p>
              <strong>Company:</strong> {job.company}
            </p>
            <p>
              <strong>Status:</strong> <span style={{ fontWeight: 'bold', color: '#0066cc' }}>{job.status}</span>
            </p>
            <p>
              <strong>Added:</strong> {new Date(job.createdAt).toLocaleDateString()}
            </p>
            <div style={{ marginTop: '10px' }}>
              <div>
                <button
                  onClick={() => handleViewJob(job.id)}
                  style={{ marginRight: '10px', padding: '8px 16px', cursor: 'pointer' }}
                >
                  View Details
                </button>
                <button
                  onClick={() => handleAnalyzeJob(job.id)}
                  disabled={analyzingJobId !== null || deletingJobId !== null}
                  aria-busy={analyzingJobId === job.id}
                  style={{
                    padding: '8px 16px',
                    minWidth: '112px',
                    cursor: analyzingJobId !== null || deletingJobId !== null ? 'wait' : 'pointer',
                    backgroundColor: '#4CAF50',
                    color: 'white',
                    opacity: (analyzingJobId !== null && analyzingJobId !== job.id) || deletingJobId !== null ? 0.6 : 1,
                  }}
                >
                  {analyzingJobId === job.id ? 'Analyzing...' : 'Analyze Job'}
                </button>
                <button
                  onClick={() => handleDeleteJob(job)}
                  disabled={deletingJobId !== null || analyzingJobId !== null}
                  aria-busy={deletingJobId === job.id}
                  style={{
                    marginLeft: '10px',
                    padding: '8px 16px',
                    minWidth: '96px',
                    cursor: deletingJobId !== null || analyzingJobId !== null ? 'wait' : 'pointer',
                    backgroundColor: '#d32f2f',
                    color: 'white',
                    opacity: (deletingJobId !== null && deletingJobId !== job.id) || analyzingJobId !== null ? 0.6 : 1,
                  }}
                >
                  {deletingJobId === job.id ? 'Deleting...' : 'Delete'}
                </button>
              </div>
              <div
                role="status"
                aria-live="polite"
                style={{ marginTop: '8px', minHeight: '24px' }}
              >
                {analyzingJobId === job.id
                  ? 'Waiting for the AI provider...'
                  : deletingJobId === job.id
                    ? 'Deleting job and its analysis data...'
                    : ''}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
