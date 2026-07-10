// API service layer for backend communication

const trimTrailingSlashes = (value: string) => value.replace(/\/+$/, '');

const API_BASE_URL = trimTrailingSlashes(import.meta.env.VITE_API_BASE_URL || 'http://localhost:5117/api');
const API_HEALTH_URL = API_BASE_URL.endsWith('/api')
  ? `${API_BASE_URL.slice(0, -4)}/health`
  : `${API_BASE_URL}/health`;

// Types
export interface Profile {
  id: number;
  summary: string;
  skills: string;
  experience: string;
}

export type JobStatus = 'Saved' | 'Applied' | 'Interview' | 'Offer' | 'Rejected';

export interface JobApplication {
  id: number;
  company: string;
  role: string;
  jobDescription: string;
  status: JobStatus;
  createdAt: string;
  analysisResults: JobAnalysisResult[];
}

export interface JobAnalysisResult {
  id: number;
  jobApplicationId: number;
  matchScore: number;
  missingSkills: string;
  strengths: string;
  suggestions: string;
  coverLetterDraft: string;
}

// Profile endpoints
export const profileAPI = {
  getProfile: async (): Promise<Profile> => {
    const response = await fetch(`${API_BASE_URL}/profile`);
    if (!response.ok) throw new Error(`Failed to fetch profile: ${response.statusText}`);
    return response.json();
  },

  saveProfile: async (profile: Omit<Profile, 'id'>): Promise<Profile> => {
    const response = await fetch(`${API_BASE_URL}/profile`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(profile),
    });
    if (!response.ok) throw new Error(`Failed to save profile: ${response.statusText}`);
    return response.json();
  },
};

// Job Application endpoints
export const jobAPI = {
  getJobs: async (): Promise<JobApplication[]> => {
    const response = await fetch(`${API_BASE_URL}/jobs`);
    if (!response.ok) throw new Error(`Failed to fetch jobs: ${response.statusText}`);
    return response.json();
  },

  getJob: async (id: number): Promise<JobApplication> => {
    const response = await fetch(`${API_BASE_URL}/jobs/${id}`);
    if (!response.ok) throw new Error(`Failed to fetch job: ${response.statusText}`);
    return response.json();
  },

  createJob: async (job: Omit<JobApplication, 'id' | 'createdAt' | 'analysisResults' | 'status'>): Promise<JobApplication> => {
    const response = await fetch(`${API_BASE_URL}/jobs`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(job),
    });
    if (!response.ok) throw new Error(`Failed to create job: ${response.statusText}`);
    return response.json();
  },

  updateJobStatus: async (id: number, status: JobStatus): Promise<JobApplication> => {
    const response = await fetch(`${API_BASE_URL}/jobs/${id}/status`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ status }),
    });
    if (!response.ok) throw new Error(`Failed to update job status: ${response.statusText}`);
    return response.json();
  },

  deleteJob: async (id: number): Promise<void> => {
    const response = await fetch(`${API_BASE_URL}/jobs/${id}`, {
      method: 'DELETE',
    });
    if (!response.ok) throw new Error(`Failed to delete job: ${response.statusText}`);
  },
};

// Analysis endpoint
export const analysisAPI = {
  analyzeJob: async (jobId: number): Promise<JobAnalysisResult> => {
    const response = await fetch(`${API_BASE_URL}/jobs/${jobId}/analyse`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
    });
    if (!response.ok) throw new Error(`Failed to analyze job: ${response.statusText}`);
    return response.json();
  },
};

// Helper function to check API availability
export const checkAPIHealth = async (): Promise<boolean> => {
  try {
    const response = await fetch(API_HEALTH_URL, {
      method: 'HEAD',
    });
    return response.ok;
  } catch {
    return false;
  }
};
