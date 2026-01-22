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

// Company Types
export interface VerifiedCompany {
    id: string;
    name: string;
    website?: string;
    taxNumber?: string;
    hrEmail?: string;
    hrPhone?: string;
    sector?: string;
    city?: string;
    description?: string;
    companyCulture?: string;
    recentNews?: string;
    isVerified: boolean;
    verifiedAt?: string;
    updatedAt: string;
}

export interface CompanyFilter {
    searchTerm?: string;
    sector?: string;
    city?: string;
    isVerified?: boolean;
    skip?: number;
    take?: number;
}

export interface CompanyStats {
    totalCompanies: number;
    verifiedCompanies: number;
    unverifiedCompanies: number;
    totalJobPostingsLinked: number;
    companiesBySector: Record<string, number>;
    companiesByCity: Record<string, number>;
}

export interface CompanyCultureAnalysis {
    culture: string;
    values: string;
    workEnvironment: string;
    benefits: string;
    careerGrowth: string;
    overallScore: number;
}

export interface CompanyNews {
    title: string;
    summary: string;
    source: string;
    url?: string;
    publishedAt: string;
}

export interface CreateCompanyRequest {
    name: string;
    website?: string;
    taxNumber?: string;
    hrEmail?: string;
    hrPhone?: string;
    sector?: string;
    city?: string;
    description?: string;
}

export interface UpdateCompanyRequest {
    name?: string;
    website?: string;
    taxNumber?: string;
    hrEmail?: string;
    hrPhone?: string;
    sector?: string;
    city?: string;
    description?: string;
    isVerified?: boolean;
}

export interface VerifyCompanyRequest {
    taxNumber?: string;
    hrEmail?: string;
    website?: string;
}

// Admin API Methods
export const adminApi = {
    // Company Management
    getCompanies: async (filter: CompanyFilter = {}): Promise<{ companies: VerifiedCompany[]; total: number }> => {
        const params = new URLSearchParams();
        if (filter.searchTerm) params.set('searchTerm', filter.searchTerm);
        if (filter.sector) params.set('sector', filter.sector);
        if (filter.city) params.set('city', filter.city);
        if (filter.isVerified !== undefined) params.set('isVerified', String(filter.isVerified));
        if (filter.skip !== undefined) params.set('skip', String(filter.skip));
        if (filter.take !== undefined) params.set('take', String(filter.take));
        
        return api.get(`/admin/companies?${params.toString()}`);
    },

    getCompany: async (id: string): Promise<VerifiedCompany> => {
        return api.get(`/admin/companies/${id}`);
    },

    createCompany: async (data: CreateCompanyRequest): Promise<VerifiedCompany> => {
        return api.post('/admin/companies', data);
    },

    updateCompany: async (id: string, data: UpdateCompanyRequest): Promise<VerifiedCompany> => {
        return api.put(`/admin/companies/${id}`, data);
    },

    deleteCompany: async (id: string): Promise<void> => {
        return api.delete(`/admin/companies/${id}`);
    },

    verifyCompany: async (id: string, data: VerifyCompanyRequest): Promise<{ message: string; company: VerifiedCompany }> => {
        return api.post(`/admin/companies/${id}/verify`, data);
    },

    analyzeCulture: async (id: string): Promise<{ message: string; analysis: CompanyCultureAnalysis }> => {
        return api.post(`/admin/companies/${id}/analyze-culture`);
    },

    scrapeNews: async (id: string): Promise<{ message: string; news: CompanyNews[]; count: number }> => {
        return api.post(`/admin/companies/${id}/scrape-news`);
    },

    checkVerification: async (companyName: string): Promise<{ isVerified: boolean; company?: VerifiedCompany }> => {
        return api.get(`/admin/companies/check-verification?companyName=${encodeURIComponent(companyName)}`);
    },

    getSectors: async (): Promise<string[]> => {
        return api.get('/admin/companies/sectors');
    },

    getCities: async (): Promise<string[]> => {
        return api.get('/admin/companies/cities');
    },

    getStats: async (): Promise<CompanyStats> => {
        return api.get('/admin/companies/stats');
    },

    seedCompanies: async (): Promise<{ message: string; totalCompanies: number }> => {
        return api.post('/admin/companies/seed');
    },
};

// Skill Gap Types
export interface SkillGap {
    id: string;
    skillName: string;
    category: string;
    subCategory: string;
    importanceLevel: number;
    description?: string;
    recommendedCourses: CourseRecommendation[];
    recommendedProjects: ProjectSuggestion[];
    recommendedCertifications: CertificationRecommendation[];
    estimatedLearningHours: number;
    status: string;
    progressPercentage: number;
    startedAt?: string;
    completedAt?: string;
    createdAt: string;
}

export interface CourseRecommendation {
    title: string;
    provider: string;
    url: string;
    level: string;
    estimatedHours: number;
    price?: number;
    rating?: number;
    description?: string;
}

export interface ProjectSuggestion {
    title: string;
    description: string;
    difficulty: string;
    technologies: string[];
    estimatedHours: number;
    gitHubTemplate?: string;
    learningOutcomes?: string;
}

export interface CertificationRecommendation {
    name: string;
    provider: string;
    url: string;
    level: string;
    cost?: number;
    validityYears?: number;
    description?: string;
    prerequisites: string[];
}

export interface SkillGapAnalysisResult {
    technicalSkills: SkillGap[];
    certifications: SkillGap[];
    experienceGaps: SkillGap[];
    softSkills: SkillGap[];
    totalGaps: number;
    completedGaps: number;
    inProgressGaps: number;
    overallReadinessScore: number;
    summary: string;
    priorityRecommendations: string[];
}

export interface SkillDevelopmentProgress {
    userId: string;
    totalSkillGaps: number;
    completedSkills: number;
    inProgressSkills: number;
    notStartedSkills: number;
    overallProgress: number;
    totalLearningHoursEstimated: number;
    totalLearningHoursCompleted: number;
    gapsByCategory: Record<string, number>;
    completedByCategory: Record<string, number>;
    recentlyCompleted: SkillGap[];
    currentlyLearning: SkillGap[];
}

export interface SkillGapFilter {
    category?: string;
    status?: string;
    minImportance?: number;
    jobMatchId?: string;
    skip?: number;
    take?: number;
}

export interface UpdateSkillGapProgress {
    status?: string;
    progressPercentage?: number;
    notes?: string;
}

// Skill Gap API Methods
export const skillGapApi = {
    // Analyze skill gaps for a job match
    analyzeForJob: async (jobMatchId: string): Promise<{ message: string; result: SkillGapAnalysisResult }> => {
        return api.post(`/skill-gaps/analyze/${jobMatchId}`);
    },

    // Analyze career gaps
    analyzeCareer: async (): Promise<{ message: string; result: SkillGapAnalysisResult }> => {
        return api.post('/skill-gaps/analyze-career');
    },

    // Get all skill gaps
    getSkillGaps: async (filter?: SkillGapFilter): Promise<{ gaps: SkillGap[]; total: number }> => {
        const params = new URLSearchParams();
        if (filter?.category) params.set('category', filter.category);
        if (filter?.status) params.set('status', filter.status);
        if (filter?.minImportance) params.set('minImportance', String(filter.minImportance));
        if (filter?.jobMatchId) params.set('jobMatchId', filter.jobMatchId);
        if (filter?.skip !== undefined) params.set('skip', String(filter.skip));
        if (filter?.take !== undefined) params.set('take', String(filter.take));
        
        return api.get(`/skill-gaps?${params.toString()}`);
    },

    // Get a specific skill gap
    getSkillGap: async (id: string): Promise<SkillGap> => {
        return api.get(`/skill-gaps/${id}`);
    },

    // Get course recommendations
    getCourseRecommendations: async (skillName: string, category?: string): Promise<{ courses: CourseRecommendation[] }> => {
        const params = category ? `?category=${encodeURIComponent(category)}` : '';
        return api.get(`/skill-gaps/courses/${encodeURIComponent(skillName)}${params}`);
    },

    // Get project suggestions
    getProjectSuggestions: async (skillName: string, category?: string): Promise<{ projects: ProjectSuggestion[] }> => {
        const params = category ? `?category=${encodeURIComponent(category)}` : '';
        return api.get(`/skill-gaps/projects/${encodeURIComponent(skillName)}${params}`);
    },

    // Get certification recommendations
    getCertificationRecommendations: async (skillName: string, category?: string): Promise<{ certifications: CertificationRecommendation[] }> => {
        const params = category ? `?category=${encodeURIComponent(category)}` : '';
        return api.get(`/skill-gaps/certifications/${encodeURIComponent(skillName)}${params}`);
    },

    // Update progress
    updateProgress: async (id: string, data: UpdateSkillGapProgress): Promise<{ message: string; skillGap: SkillGap }> => {
        return api.put(`/skill-gaps/${id}/progress`, data);
    },

    // Mark as completed
    markAsCompleted: async (id: string): Promise<{ message: string; skillGap: SkillGap }> => {
        return api.post(`/skill-gaps/${id}/complete`);
    },

    // Get development progress
    getDevelopmentProgress: async (): Promise<SkillDevelopmentProgress> => {
        return api.get('/skill-gaps/progress');
    },

    // Delete skill gap
    deleteSkillGap: async (id: string): Promise<void> => {
        return api.delete(`/skill-gaps/${id}`);
    },

    // Recalculate match score
    recalculateMatchScore: async (jobMatchId: string): Promise<{ message: string; newScore: number }> => {
        return api.post(`/skill-gaps/recalculate-match/${jobMatchId}`);
    },
};