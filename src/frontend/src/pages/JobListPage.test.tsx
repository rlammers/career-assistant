import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ToastProvider } from '../components/Toast';
import { JobListPage } from './JobListPage';
import { analysisAPI, jobAPI } from '../services/api';

vi.mock('../services/api', () => ({
  jobAPI: { getJobs: vi.fn(), createJob: vi.fn(), deleteJob: vi.fn() },
  analysisAPI: { analyzeJob: vi.fn() },
}));

const job = {
  id: 1,
  company: 'Example Co',
  role: 'Developer',
  jobDescription: 'Build useful software',
  status: 'Saved' as const,
  createdAt: '2026-07-10T00:00:00Z',
  analysisResults: [],
};

const renderPage = () => render(
  <ToastProvider><MemoryRouter><JobListPage /></MemoryRouter></ToastProvider>,
);

describe('JobListPage feedback', () => {
  beforeEach(() => vi.clearAllMocks());

  it('uses a toast for successful job creation and keeps failures inline', async () => {
    vi.mocked(jobAPI.getJobs).mockResolvedValue([]);
    vi.mocked(jobAPI.createJob).mockRejectedValueOnce(new Error('Could not save')).mockResolvedValueOnce(job);
    renderPage();

    fireEvent.click(await screen.findByRole('button', { name: 'Add New Job' }));
    fireEvent.change(screen.getByLabelText('Company'), { target: { value: job.company } });
    fireEvent.change(screen.getByLabelText('Role'), { target: { value: job.role } });
    fireEvent.change(screen.getByLabelText('Job Description'), { target: { value: job.jobDescription } });
    fireEvent.click(screen.getByRole('button', { name: 'Add Job' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Could not save');

    fireEvent.click(screen.getByRole('button', { name: 'Add Job' }));
    expect(await screen.findByText('Job added successfully.')).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('shows analysis success and a warning when the background refresh fails', async () => {
    vi.mocked(jobAPI.getJobs).mockResolvedValueOnce([job]).mockRejectedValueOnce(new Error('offline'));
    vi.mocked(analysisAPI.analyzeJob).mockResolvedValue({
      id: 1, jobApplicationId: 1, matchScore: 80, missingSkills: '', strengths: '', suggestions: '', coverLetterDraft: '',
    });
    renderPage();

    fireEvent.click(await screen.findByRole('button', { name: 'Analyze Job' }));
    expect(await screen.findByText('Job analyzed. View its details to see the analysis.')).toBeInTheDocument();
    expect(await screen.findByText(/job list could not refresh/i)).toBeInTheDocument();
    expect(screen.getByText('Developer')).toBeInTheDocument();
  });

  it('keeps analysis failures inline with the affected job', async () => {
    vi.mocked(jobAPI.getJobs).mockResolvedValue([job]);
    vi.mocked(analysisAPI.analyzeJob).mockRejectedValue(new Error('Analysis unavailable'));
    renderPage();

    fireEvent.click(await screen.findByRole('button', { name: 'Analyze Job' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Analysis unavailable');
    expect(screen.queryByText(/Job analyzed/)).not.toBeInTheDocument();
  });

  it('requires an in-page decision before deleting and supports cancellation', async () => {
    vi.mocked(jobAPI.getJobs).mockResolvedValue([job]);
    vi.mocked(jobAPI.deleteJob).mockResolvedValue();
    renderPage();

    fireEvent.click(await screen.findByRole('button', { name: 'Delete' }));
    expect(screen.getByRole('group', { name: /Confirm deletion/ })).toHaveTextContent('cannot be undone');
    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(screen.queryByRole('group', { name: /Confirm deletion/ })).not.toBeInTheDocument();
    expect(jobAPI.deleteJob).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole('button', { name: 'Delete' }));
    fireEvent.click(screen.getByRole('button', { name: 'Delete job' }));
    await waitFor(() => expect(jobAPI.deleteJob).toHaveBeenCalledWith(1));
    expect(await screen.findByText('Developer at Example Co deleted.')).toBeInTheDocument();
  });
});
