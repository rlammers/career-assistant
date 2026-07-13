import { fireEvent, render, screen } from '@testing-library/react';
import { BrowserRouter, MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ProfileGate } from './App';
import { ToastProvider } from './components/Toast';
import { ApiError, jobAPI, profileAPI } from './services/api';

vi.mock('./services/api', async (importOriginal) => {
  const actual = await importOriginal<typeof import('./services/api')>();
  return {
    ...actual,
    profileAPI: { getProfile: vi.fn(), saveProfile: vi.fn() },
    jobAPI: { getJobs: vi.fn() },
  };
});

describe('profile-first workflow gate', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(jobAPI.getJobs).mockResolvedValue([]);
  });

  it('keeps the jobs workflow unavailable until a profile exists', async () => {
    vi.mocked(profileAPI.getProfile).mockRejectedValue(new ApiError('Profile not found', 404));
    render(<BrowserRouter><ToastProvider><ProfileGate /></ToastProvider></BrowserRouter>);

    expect(await screen.findByRole('heading', { name: 'My Profile' })).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Jobs' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Analyze Job' })).not.toBeInTheDocument();
    expect(jobAPI.getJobs).not.toHaveBeenCalled();
  });

  it('unlocks and navigates to jobs after the first profile save', async () => {
    vi.mocked(profileAPI.getProfile).mockRejectedValue(new ApiError('Profile not found', 404));
    vi.mocked(profileAPI.saveProfile).mockResolvedValue({ id: 1, summary: 'Summary', skills: 'React', experience: 'Engineer' });
    render(<BrowserRouter><ToastProvider><ProfileGate /></ToastProvider></BrowserRouter>);

    await screen.findByRole('heading', { name: 'My Profile' });
    fireEvent.change(screen.getByLabelText('Professional Summary'), { target: { value: 'Summary' } });
    fireEvent.change(screen.getByLabelText('Skills (comma-separated)'), { target: { value: 'React' } });
    fireEvent.change(screen.getByLabelText('Experience'), { target: { value: 'Engineer' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save Profile' }));

    expect(await screen.findByRole('heading', { name: 'Job Applications' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Jobs' })).toBeInTheDocument();
    expect(jobAPI.getJobs).toHaveBeenCalled();
  });

  it('shows profile-load errors instead of treating them as a missing profile', async () => {
    vi.mocked(profileAPI.getProfile).mockRejectedValue(new ApiError('Failed to fetch profile: Server error', 500));
    render(<BrowserRouter><ToastProvider><ProfileGate /></ToastProvider></BrowserRouter>);

    expect(await screen.findByRole('alert')).toHaveTextContent('Failed to fetch profile: Server error');
    expect(screen.queryByRole('heading', { name: 'My Profile' })).not.toBeInTheDocument();
  });

  it('keeps edited profile values after navigating away and back', async () => {
    const initialProfile = {
      id: 1,
      summary: 'Original summary',
      skills: 'Original skills',
      experience: 'Original experience',
    };
    const updatedProfile = {
      id: 1,
      summary: 'Updated summary',
      skills: 'Updated skills',
      experience: 'Updated experience',
    };
    vi.mocked(profileAPI.getProfile).mockResolvedValue(initialProfile);
    vi.mocked(profileAPI.saveProfile).mockResolvedValue(updatedProfile);
    render(<MemoryRouter initialEntries={['/']}><ToastProvider><ProfileGate /></ToastProvider></MemoryRouter>);

    expect(await screen.findByRole('heading', { name: 'Job Applications' })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('link', { name: 'Profile' }));
    expect(await screen.findByDisplayValue(initialProfile.summary)).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('Professional Summary'), { target: { value: updatedProfile.summary } });
    fireEvent.change(screen.getByLabelText('Skills (comma-separated)'), { target: { value: updatedProfile.skills } });
    fireEvent.change(screen.getByLabelText('Experience'), { target: { value: updatedProfile.experience } });
    fireEvent.click(screen.getByRole('button', { name: 'Save Profile' }));
    expect(await screen.findByText('Profile saved successfully.')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('link', { name: 'Jobs' }));
    expect(await screen.findByRole('heading', { name: 'Job Applications' })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('link', { name: 'Profile' }));

    expect(await screen.findByDisplayValue(updatedProfile.summary)).toBeInTheDocument();
    expect(screen.queryByDisplayValue(initialProfile.summary)).not.toBeInTheDocument();
    expect(screen.getByDisplayValue(updatedProfile.skills)).toBeInTheDocument();
    expect(screen.getByDisplayValue(updatedProfile.experience)).toBeInTheDocument();
    expect(profileAPI.saveProfile).toHaveBeenCalledWith({
      summary: updatedProfile.summary,
      skills: updatedProfile.skills,
      experience: updatedProfile.experience,
    });
  });
});
