import { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { jobAPI } from '../services/api';
import type { JobApplication, JobAnalysisResult, JobStatus } from '../services/api';
import { InlineError } from '../components/InlineError';
import { useToast } from '../components/ToastContext';

export const JobDetailPage = () => {
  const { showToast } = useToast();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [job, setJob] = useState<JobApplication | null>(null);
  const [analysis, setAnalysis] = useState<JobAnalysisResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [statusError, setStatusError] = useState<string | null>(null);
  const [statusDropdown, setStatusDropdown] = useState<JobStatus>('Saved');
  const [updatingStatus, setUpdatingStatus] = useState(false);
  const [editing, setEditing] = useState(false);
  const [saving, setSaving] = useState(false);
  const [editCompany, setEditCompany] = useState('');
  const [editRole, setEditRole] = useState('');
  const [editJobDescription, setEditJobDescription] = useState('');

  const fetchJobDetails = useCallback(async () => {
    if (!id) return;
    setLoading(true);
    setError(null);
    try {
      const jobData = await jobAPI.getJob(parseInt(id, 10));
      setJob(jobData);
      setStatusDropdown(jobData.status);

      // Get latest analysis if available
      if (jobData.analysisResults && jobData.analysisResults.length > 0) {
        setAnalysis(jobData.analysisResults[jobData.analysisResults.length - 1]);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch job details');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    fetchJobDetails();
  }, [fetchJobDetails]);

  const handleStatusChange = async () => {
    if (!job || statusDropdown === job.status) return;

    setUpdatingStatus(true);
    setStatusError(null);
    try {
      const updatedJob = await jobAPI.updateJobStatus(job.id, statusDropdown);
      setJob(updatedJob);
      showToast({ message: 'Status updated successfully.', variant: 'success' });
    } catch (err) {
      setStatusError(err instanceof Error ? err.message : 'Failed to update status');
      setStatusDropdown(job.status); // Revert dropdown
    } finally {
      setUpdatingStatus(false);
    }
  };

  const beginEditing = () => {
    if (!job) return;
    setEditCompany(job.company);
    setEditRole(job.role);
    setEditJobDescription(job.jobDescription);
    setError(null);
    setEditing(true);
  };

  const handleSave = async () => {
    if (!job) return;

    setSaving(true);
    setError(null);
    try {
      const updatedJob = await jobAPI.updateJob(job.id, {
        company: editCompany,
        role: editRole,
        jobDescription: editJobDescription,
      });
      setJob(updatedJob);
      setEditing(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update job');
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <div style={{ padding: '20px' }}>Loading job details...</div>;
  if (error && !job) return <div style={{ padding: '20px', color: 'red' }}>Error: {error}</div>;
  if (!job) return <div style={{ padding: '20px' }}>Job not found.</div>;

  return (
    <div style={{ maxWidth: '900px', margin: '0 auto', padding: '20px' }}>
      <button onClick={() => navigate('/jobs')} style={{ marginBottom: '20px', padding: '8px 16px', cursor: 'pointer' }}>
        ← Back to Jobs
      </button>

      <div style={{ backgroundColor: '#f9f9f9', padding: '20px', borderRadius: '4px', marginBottom: '20px' }}>
        {editing ? (
          <>
            <label style={{ display: 'block', marginBottom: '12px' }}>
              <strong>Role</strong>
              <input value={editRole} onChange={(event) => setEditRole(event.target.value)} style={{ display: 'block', width: '100%', boxSizing: 'border-box', padding: '8px', marginTop: '4px' }} />
            </label>
            <label style={{ display: 'block', marginBottom: '12px' }}>
              <strong>Company</strong>
              <input value={editCompany} onChange={(event) => setEditCompany(event.target.value)} style={{ display: 'block', width: '100%', boxSizing: 'border-box', padding: '8px', marginTop: '4px' }} />
            </label>
          </>
        ) : (
          <>
            <h1>{job.role}</h1>
            <p><strong>Company:</strong> {job.company}</p>
          </>
        )}
        <p>
          <strong>Status:</strong>
          <select
            value={statusDropdown}
            onChange={(e) => setStatusDropdown(e.target.value as JobStatus)}
            style={{ marginLeft: '10px', padding: '5px' }}
          >
            <option value="Saved">Saved</option>
            <option value="Applied">Applied</option>
            <option value="Interview">Interview</option>
            <option value="Offer">Offer</option>
            <option value="Rejected">Rejected</option>
          </select>
          <button
            onClick={handleStatusChange}
            disabled={updatingStatus || statusDropdown === job.status}
            style={{ marginLeft: '10px', padding: '5px 15px', cursor: 'pointer' }}
          >
            {updatingStatus ? 'Updating...' : 'Update Status'}
          </button>
        </p>
        <InlineError message={statusError} />
        <p>
          <strong>Added:</strong> {new Date(job.createdAt).toLocaleDateString()}
        </p>
        <div style={{ minHeight: '38px' }}>
          {editing ? (
            <>
              <button onClick={handleSave} disabled={saving} style={{ padding: '8px 16px', marginRight: '8px', cursor: 'pointer' }}>{saving ? 'Saving...' : 'Save Changes'}</button>
              <button onClick={() => { setEditing(false); setError(null); }} disabled={saving} style={{ padding: '8px 16px', cursor: 'pointer' }}>Cancel</button>
            </>
          ) : (
            <button onClick={beginEditing} style={{ padding: '8px 16px', cursor: 'pointer' }}>Edit Job</button>
          )}
        </div>
        <div role="status" style={{ minHeight: '24px', color: '#b00020', marginTop: '4px' }}>{error ?? ''}</div>
      </div>

      <div style={{ marginBottom: '20px' }}>
        <h2>Job Description</h2>
        {editing ? (
          <textarea
            value={editJobDescription}
            onChange={(event) => setEditJobDescription(event.target.value)}
            rows={14}
            style={{ width: '100%', boxSizing: 'border-box', padding: '15px', fontFamily: 'monospace', fontSize: '12px', resize: 'vertical' }}
          />
        ) : <div
          style={{
            backgroundColor: '#fafafa',
            padding: '15px',
            borderRadius: '4px',
            whiteSpace: 'pre-wrap',
            fontFamily: 'monospace',
            fontSize: '12px',
            maxHeight: '300px',
            overflowY: 'auto',
          }}
        >
          {job.jobDescription}
        </div>}
      </div>

      {analysis ? (
        <div style={{ marginBottom: '20px' }}>
          <h2>AI Analysis</h2>

          <div style={{ marginBottom: '15px', padding: '15px', backgroundColor: '#e8f5e9', borderRadius: '4px' }}>
            <h3>Match Score: {analysis.matchScore}%</h3>
            <div style={{ width: '100%', backgroundColor: '#ddd', borderRadius: '4px', overflow: 'hidden' }}>
              <div
                style={{
                  width: `${analysis.matchScore}%`,
                  height: '20px',
                  backgroundColor: analysis.matchScore >= 70 ? '#4CAF50' : analysis.matchScore >= 50 ? '#FFC107' : '#f44336',
                }}
              />
            </div>
          </div>

          <div style={{ marginBottom: '15px', padding: '15px', backgroundColor: '#e3f2fd', borderRadius: '4px' }}>
            <h3>Strengths</h3>
            <p>{analysis.strengths}</p>
          </div>

          <div style={{ marginBottom: '15px', padding: '15px', backgroundColor: '#fff3e0', borderRadius: '4px' }}>
            <h3>Missing Skills</h3>
            <p>{analysis.missingSkills}</p>
          </div>

          <div style={{ marginBottom: '15px', padding: '15px', backgroundColor: '#f3e5f5', borderRadius: '4px' }}>
            <h3>Suggestions</h3>
            <p>{analysis.suggestions}</p>
          </div>

          <div style={{ marginBottom: '15px', padding: '15px', backgroundColor: '#fce4ec', borderRadius: '4px' }}>
            <h3>Cover Letter Draft</h3>
            <p style={{ fontStyle: 'italic' }}>{analysis.coverLetterDraft}</p>
          </div>
        </div>
      ) : (
        <div style={{ padding: '15px', backgroundColor: '#fff3cd', borderRadius: '4px' }}>
          <p>No analysis yet. Go back to the job list and click "Analyze Job" to generate an analysis.</p>
        </div>
      )}
    </div>
  );
};
