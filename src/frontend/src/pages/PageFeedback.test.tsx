import { fireEvent, render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ToastProvider } from '../components/Toast';
import { jobAPI, profileAPI } from '../services/api';
import { JobDetailPage } from './JobDetailPage';
import { ProfilePage } from './ProfilePage';

vi.mock('../services/api', () => ({
  profileAPI: { getProfile: vi.fn(), saveProfile: vi.fn() },
  jobAPI: { getJob: vi.fn(), updateJobStatus: vi.fn() },
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

describe('page action feedback', () => {
  beforeEach(() => vi.clearAllMocks());

  it('uses a toast for profile-save success and an inline error for failure', async () => {
    vi.mocked(profileAPI.getProfile).mockRejectedValue(new Error('No profile'));
    vi.mocked(profileAPI.saveProfile)
      .mockRejectedValueOnce(new Error('Profile could not be saved'))
      .mockResolvedValueOnce({ id: 1, summary: '', skills: '', experience: '' });
    render(<ToastProvider><ProfilePage /></ToastProvider>);

    fireEvent.click(await screen.findByRole('button', { name: 'Save Profile' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Profile could not be saved');
    fireEvent.click(screen.getByRole('button', { name: 'Save Profile' }));
    expect(await screen.findByText('Profile saved successfully.')).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('uses a toast for status success and reverts the selection after an inline failure', async () => {
    vi.mocked(jobAPI.getJob).mockResolvedValue(job);
    vi.mocked(jobAPI.updateJobStatus)
      .mockRejectedValueOnce(new Error('Status could not be updated'))
      .mockResolvedValueOnce({ ...job, status: 'Applied' });
    render(
      <ToastProvider>
        <MemoryRouter initialEntries={['/jobs/1']}>
          <Routes><Route path="/jobs/:id" element={<JobDetailPage />} /></Routes>
        </MemoryRouter>
      </ToastProvider>,
    );

    const select = await screen.findByRole('combobox');
    fireEvent.change(select, { target: { value: 'Applied' } });
    fireEvent.click(screen.getByRole('button', { name: 'Update Status' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Status could not be updated');
    expect(select).toHaveValue('Saved');

    fireEvent.change(select, { target: { value: 'Applied' } });
    fireEvent.click(screen.getByRole('button', { name: 'Update Status' }));
    expect(await screen.findByText('Status updated successfully.')).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });
});
