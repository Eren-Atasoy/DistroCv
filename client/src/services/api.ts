// API Base URL
const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

// API Client with error handling
class ApiClient {
    private baseUrl: string;

    constructor(baseUrl: string) {
        this.baseUrl = baseUrl;
    }

    private async request<T>(
        endpoint: string,
        options: RequestInit = {}
    ): Promise<T> {
        const url = `${this.baseUrl}${endpoint}`;

        const config: RequestInit = {
            ...options,
            headers: {
                'Content-Type': 'application/json',
                ...options.headers,
            },
        };

        try {
            const response = await fetch(url, config);

            if (!response.ok) {
                const error = await response.json().catch(() => ({
                    message: response.statusText,
                }));
                throw new Error(error.message || 'API request failed');
            }

            return await response.json();
        } catch (error) {
            console.error('API Error:', error);
            throw error;
        }
    }

    async get<T>(endpoint: string): Promise<T> {
        return this.request<T>(endpoint, { method: 'GET' });
    }

    async post<T>(endpoint: string, data?: unknown): Promise<T> {
        return this.request<T>(endpoint, {
            method: 'POST',
            body: data ? JSON.stringify(data) : undefined,
        });
    }

    async put<T>(endpoint: string, data?: unknown): Promise<T> {
        return this.request<T>(endpoint, {
            method: 'PUT',
            body: data ? JSON.stringify(data) : undefined,
        });
    }

    async delete<T>(endpoint: string): Promise<T> {
        return this.request<T>(endpoint, { method: 'DELETE' });
    }
}

export const api = new ApiClient(API_BASE_URL);

// Types
export interface JobMatch {
    id: string;
    jobPostingId: string;
    userId: string;
    matchScore: number;
    matchReasoning: string;
    skillGaps: string[];
    createdAt: string;
    jobPosting: JobPosting;
}

export interface JobPosting {
    id: string;
    title: string;
    companyName: string;
    location: string;
    description: string;
    requirements: string;
    salary?: string;
    postedDate: string;
    externalUrl?: string;
}

export interface FeedbackRequest {
    jobMatchId: string;
    feedbackType: 'Reject' | 'Approve';
    reason?: string;
    notes?: string;
}

// API Methods
export const jobsApi = {
    // Get matched jobs for user
    getMatchedJobs: async (): Promise<JobMatch[]> => {
        return api.get<JobMatch[]>('/jobs/matched');
    },

    // Approve a job match
    approveMatch: async (jobMatchId: string): Promise<void> => {
        return api.post(`/jobs/${jobMatchId}/approve`);
    },

    // Reject a job match with feedback
    rejectMatch: async (jobMatchId: string, reason?: string): Promise<void> => {
        return api.post(`/jobs/${jobMatchId}/reject`, { reason });
    },
};

export const feedbackApi = {
    // Submit feedback for a job match
    submitFeedback: async (feedback: FeedbackRequest): Promise<void> => {
        return api.post('/feedback', feedback);
    },
};
