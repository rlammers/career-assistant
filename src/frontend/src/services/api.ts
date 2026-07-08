// API service layer for backend communication
// Base URL: http://localhost:5000/api

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api';

// Types
export interface Profile {
  id: number;
  summary: string;
  skills: string;
  experience: string;
}

export interface JobApplication {
  id: number;
  company: string;
  role: string;
  jobDescription: string;
  status: string;
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

  updateJobStatus: async (id: number, status: string): Promise<JobApplication> => {
    const response = await fetch(`${API_BASE_URL}/jobs/${id}/status`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ status }),
    });
    if (!response.ok) throw new Error(`Failed to update job status: ${response.statusText}`);
    return response.json();
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
    const response = await fetch(`${API_BASE_URL.replace('/api', '')}/health`, {
      method: 'HEAD',
    });
    return response.ok;
  } catch {
    return false;
  }
};
