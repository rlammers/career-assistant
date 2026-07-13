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

const profile = {
  id: 1,
  summary: 'Saved summary',
  skills: 'Saved skills',
  experience: 'Saved experience',
};

describe('page action feedback', () => {
  beforeEach(() => vi.clearAllMocks());

  it('uses a toast for profile-save success and an inline error for failure', async () => {
    vi.mocked(profileAPI.getProfile).mockRejectedValue(new Error('No profile'));
    vi.mocked(profileAPI.saveProfile)
      .mockRejectedValueOnce(new Error('Profile could not be saved'))
      .mockResolvedValueOnce({ id: 1, summary: '', skills: '', experience: '' });
    render(<ToastProvider><ProfilePage /></ToastProvider>);

    fireEvent.change(screen.getByLabelText('Professional Summary'), { target: { value: 'Summary' } });
    fireEvent.change(screen.getByLabelText('Skills (comma-separated)'), { target: { value: 'React' } });
    fireEvent.change(screen.getByLabelText('Experience'), { target: { value: 'Engineer' } });
    fireEvent.click(await screen.findByRole('button', { name: 'Save Profile' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Profile could not be saved');
    fireEvent.click(screen.getByRole('button', { name: 'Save Profile' }));
    expect(await screen.findByText('Profile saved successfully.')).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('requires all profile fields before saving', async () => {
    vi.mocked(profileAPI.getProfile).mockRejectedValue(new Error('No profile'));
    render(<ToastProvider><ProfilePage /></ToastProvider>);

    const saveButton = await screen.findByRole('button', { name: 'Save Profile' });
    fireEvent.submit(saveButton.closest('form')!);

    expect(await screen.findByRole('alert')).toHaveTextContent('Summary, skills, and experience are required');
    expect(profileAPI.saveProfile).not.toHaveBeenCalled();
  });

  it('initializes existing profile fields without fetching again', async () => {
    render(<ToastProvider><ProfilePage initialProfile={profile} /></ToastProvider>);

    expect(await screen.findByDisplayValue(profile.summary)).toBeInTheDocument();
    expect(screen.getByDisplayValue(profile.skills)).toBeInTheDocument();
    expect(screen.getByDisplayValue(profile.experience)).toBeInTheDocument();
    expect(profileAPI.getProfile).not.toHaveBeenCalled();
  });

  it('synchronizes form fields when the supplied profile meaningfully changes', async () => {
    const { rerender } = render(<ToastProvider><ProfilePage initialProfile={profile} /></ToastProvider>);
    const updatedProfile = {
      id: 1,
      summary: 'Updated summary',
      skills: 'Updated skills',
      experience: 'Updated experience',
    };

    rerender(<ToastProvider><ProfilePage initialProfile={updatedProfile} /></ToastProvider>);

    expect(await screen.findByDisplayValue(updatedProfile.summary)).toBeInTheDocument();
    expect(screen.getByDisplayValue(updatedProfile.skills)).toBeInTheDocument();
    expect(screen.getByDisplayValue(updatedProfile.experience)).toBeInTheDocument();
  });

  it('keeps a null initial profile empty and reports the first saved profile', async () => {
    const onProfileSaved = vi.fn();
    const savedProfile = { ...profile, summary: 'New summary' };
    vi.mocked(profileAPI.saveProfile).mockResolvedValue(savedProfile);
    render(<ToastProvider><ProfilePage initialProfile={null} onProfileSaved={onProfileSaved} /></ToastProvider>);

    expect(screen.getByLabelText('Professional Summary')).toHaveValue('');
    expect(screen.getByLabelText('Skills (comma-separated)')).toHaveValue('');
    expect(screen.getByLabelText('Experience')).toHaveValue('');

    fireEvent.change(screen.getByLabelText('Professional Summary'), { target: { value: 'New summary' } });
    fireEvent.change(screen.getByLabelText('Skills (comma-separated)'), { target: { value: 'Skills' } });
    fireEvent.change(screen.getByLabelText('Experience'), { target: { value: 'Experience' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save Profile' }));

    expect(await screen.findByText('Profile saved successfully.')).toBeInTheDocument();
    expect(onProfileSaved).toHaveBeenCalledWith(savedProfile);
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
