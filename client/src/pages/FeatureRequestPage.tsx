import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import {
  LightBulbIcon,
  PlusIcon,
  FunnelIcon,
  CheckCircleIcon,
  ClockIcon,
  ChevronUpIcon,
  ChatBubbleLeftIcon,
  RocketLaunchIcon,
} from '@heroicons/react/24/outline';

interface FeatureRequest {
  id: string;
  title: string;
  description: string;
  useCase?: string;
  category: string;
  priority: 'Critical' | 'High' | 'Medium' | 'Low';
  status: 'Submitted' | 'UnderReview' | 'Planned' | 'InProgress' | 'Testing' | 'Completed' | 'Rejected' | 'Deferred';
  requesterName?: string;
  createdAt: string;
  voteCount: number;
  commentCount: number;
  targetVersion?: string;
  hasVoted: boolean;
}

const FeatureRequestPage: React.FC = () => {
  const { t } = useTranslation();
  const [features, setFeatures] = useState<FeatureRequest[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [selectedStatus, setSelectedStatus] = useState<string>('all');
  const [selectedCategory, setSelectedCategory] = useState<string>('all');

  const [newFeature, setNewFeature] = useState({
    title: '',
    description: '',
    useCase: '',
    expectedBehavior: '',
    category: 'Other',
  });

  const categories = [
    'ResumeManagement', 'JobMatching', 'ApplicationProcess', 'InterviewPreparation',
    'Analytics', 'Notifications', 'Integration', 'Localization', 'Accessibility',
    'Performance', 'Security', 'MobileApp', 'Automation', 'AIFeatures', 'Other'
  ];

  const statuses = [
    'Submitted', 'UnderReview', 'Planned', 'InProgress', 'Testing', 'Completed', 'Rejected', 'Deferred'
  ];

  useEffect(() => {
    fetchFeatures();
  }, [selectedStatus, selectedCategory]);

  const fetchFeatures = async () => {
    try {
      setIsLoading(true);
      const params = new URLSearchParams();
      if (selectedStatus !== 'all') params.append('status', selectedStatus);
      if (selectedCategory !== 'all') params.append('category', selectedCategory);

      const response = await fetch(`/api/betatesting/features?${params}`, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
        },
      });

      if (response.ok) {
        const data = await response.json();
        setFeatures(data);
      }
    } catch (error) {
      console.error('Error fetching features:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleSubmitFeature = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const response = await fetch('/api/betatesting/features', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
        },
        body: JSON.stringify(newFeature),
      });

      if (response.ok) {
        setShowCreateModal(false);
        setNewFeature({
          title: '',
          description: '',
          useCase: '',
          expectedBehavior: '',
          category: 'Other',
        });
        fetchFeatures();
      }
    } catch (error) {
      console.error('Error submitting feature:', error);
    }
  };

  const handleVote = async (featureId: string, hasVoted: boolean) => {
    try {
      await fetch(`/api/betatesting/features/${featureId}/vote`, {
        method: hasVoted ? 'DELETE' : 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
        },
      });
      fetchFeatures();
    } catch (error) {
      console.error('Error voting:', error);
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Submitted': return 'bg-slate-100 text-slate-800 border-slate-200';
      case 'UnderReview': return 'bg-blue-100 text-blue-800 border-blue-200';
      case 'Planned': return 'bg-purple-100 text-purple-800 border-purple-200';
      case 'InProgress': return 'bg-yellow-100 text-yellow-800 border-yellow-200';
      case 'Completed': return 'bg-green-100 text-green-800 border-green-200';
      case 'Rejected': return 'bg-red-100 text-red-800 border-red-200';
      default: return 'bg-gray-100 text-gray-800 border-gray-200';
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'Completed': return <CheckCircleIcon className="w-4 h-4 text-green-500" />;
      case 'InProgress': return <RocketLaunchIcon className="w-4 h-4 text-yellow-500" />;
      case 'Planned': return <ClockIcon className="w-4 h-4 text-purple-500" />;
      default: return <ClockIcon className="w-4 h-4 text-slate-400" />;
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 text-white p-6">
      {/* Header */}
      <div className="flex items-center justify-between mb-8">
        <div className="flex items-center gap-3">
          <div className="p-3 bg-yellow-500/20 rounded-xl">
            <LightBulbIcon className="w-8 h-8 text-yellow-400" />
          </div>
          <div>
            <h1 className="text-2xl font-bold">Feature Requests</h1>
            <p className="text-slate-400">Vote and suggest new features</p>
          </div>
        </div>
        <button
          onClick={() => setShowCreateModal(true)}
          className="flex items-center gap-2 px-4 py-2 bg-yellow-500 hover:bg-yellow-600 text-black rounded-lg transition-colors"
        >
          <PlusIcon className="w-5 h-5" />
          Request Feature
        </button>
      </div>

      {/* Filters */}
      <div className="flex gap-4 mb-6">
        <div className="flex items-center gap-2">
          <FunnelIcon className="w-5 h-5 text-slate-400" />
          <select
            value={selectedStatus}
            onChange={(e) => setSelectedStatus(e.target.value)}
            className="bg-slate-700 border border-slate-600 rounded-lg px-3 py-2 text-sm"
          >
            <option value="all">All Status</option>
            {statuses.map(status => (
              <option key={status} value={status}>{status}</option>
            ))}
          </select>
        </div>
        <select
          value={selectedCategory}
          onChange={(e) => setSelectedCategory(e.target.value)}
          className="bg-slate-700 border border-slate-600 rounded-lg px-3 py-2 text-sm"
        >
          <option value="all">All Categories</option>
          {categories.map(category => (
            <option key={category} value={category}>{category}</option>
          ))}
        </select>
      </div>

      {/* Feature List */}
      {isLoading ? (
        <div className="flex justify-center py-12">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-yellow-500"></div>
        </div>
      ) : features.length === 0 ? (
        <div className="text-center py-12">
          <LightBulbIcon className="w-16 h-16 text-slate-600 mx-auto mb-4" />
          <p className="text-slate-400">No feature requests yet. Be the first to suggest!</p>
        </div>
      ) : (
        <div className="space-y-4">
          {features.map((feature) => (
            <div
              key={feature.id}
              className="bg-slate-800/50 border border-slate-700 rounded-xl p-6 hover:border-slate-600 transition-colors"
            >
              <div className="flex items-start gap-4">
                {/* Vote Button */}
                <button
                  onClick={() => handleVote(feature.id, feature.hasVoted)}
                  className={`flex flex-col items-center p-2 rounded-lg transition-colors ${
                    feature.hasVoted
                      ? 'bg-yellow-500/20 text-yellow-400'
                      : 'bg-slate-700 text-slate-400 hover:bg-slate-600'
                  }`}
                >
                  <ChevronUpIcon className="w-5 h-5" />
                  <span className="text-sm font-semibold">{feature.voteCount}</span>
                </button>

                {/* Content */}
                <div className="flex-1">
                  <div className="flex items-center gap-3 mb-2">
                    {getStatusIcon(feature.status)}
                    <h3 className="text-lg font-semibold">{feature.title}</h3>
                    <span className={`text-xs px-2 py-1 rounded-full border ${getStatusColor(feature.status)}`}>
                      {feature.status}
                    </span>
                    {feature.targetVersion && (
                      <span className="text-xs px-2 py-1 bg-purple-500/20 text-purple-300 rounded-full">
                        v{feature.targetVersion}
                      </span>
                    )}
                  </div>
                  <p className="text-slate-400 text-sm mb-3 line-clamp-2">{feature.description}</p>
                  {feature.useCase && (
                    <p className="text-slate-500 text-sm mb-3 italic">
                      Use case: {feature.useCase}
                    </p>
                  )}
                  <div className="flex items-center gap-4 text-sm text-slate-500">
                    <span className="px-2 py-0.5 bg-slate-700 rounded">{feature.category}</span>
                    <span>•</span>
                    <span>{new Date(feature.createdAt).toLocaleDateString()}</span>
                    {feature.requesterName && (
                      <>
                        <span>•</span>
                        <span>by {feature.requesterName}</span>
                      </>
                    )}
                    <span>•</span>
                    <div className="flex items-center gap-1">
                      <ChatBubbleLeftIcon className="w-4 h-4" />
                      <span>{feature.commentCount}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Create Feature Modal */}
      {showCreateModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-slate-800 rounded-2xl p-6 max-w-2xl w-full mx-4 max-h-[90vh] overflow-y-auto">
            <h2 className="text-xl font-bold mb-4 flex items-center gap-2">
              <LightBulbIcon className="w-6 h-6 text-yellow-400" />
              Request a Feature
            </h2>
            <form onSubmit={handleSubmitFeature} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-1">Title *</label>
                <input
                  type="text"
                  required
                  value={newFeature.title}
                  onChange={(e) => setNewFeature({ ...newFeature, title: e.target.value })}
                  className="w-full bg-slate-700 border border-slate-600 rounded-lg px-4 py-2 focus:outline-none focus:border-yellow-500"
                  placeholder="Brief description of the feature"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-300 mb-1">Description *</label>
                <textarea
                  required
                  rows={4}
                  value={newFeature.description}
                  onChange={(e) => setNewFeature({ ...newFeature, description: e.target.value })}
                  className="w-full bg-slate-700 border border-slate-600 rounded-lg px-4 py-2 focus:outline-none focus:border-yellow-500"
                  placeholder="Detailed description of the feature you'd like to see"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-300 mb-1">Use Case</label>
                <textarea
                  rows={2}
                  value={newFeature.useCase}
                  onChange={(e) => setNewFeature({ ...newFeature, useCase: e.target.value })}
                  className="w-full bg-slate-700 border border-slate-600 rounded-lg px-4 py-2 focus:outline-none focus:border-yellow-500"
                  placeholder="Why do you need this feature? What problem does it solve?"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-300 mb-1">Expected Behavior</label>
                <textarea
                  rows={2}
                  value={newFeature.expectedBehavior}
                  onChange={(e) => setNewFeature({ ...newFeature, expectedBehavior: e.target.value })}
                  className="w-full bg-slate-700 border border-slate-600 rounded-lg px-4 py-2 focus:outline-none focus:border-yellow-500"
                  placeholder="How should this feature work?"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-300 mb-1">Category</label>
                <select
                  value={newFeature.category}
                  onChange={(e) => setNewFeature({ ...newFeature, category: e.target.value })}
                  className="w-full bg-slate-700 border border-slate-600 rounded-lg px-4 py-2"
                >
                  {categories.map(category => (
                    <option key={category} value={category}>{category}</option>
                  ))}
                </select>
              </div>

              <div className="flex justify-end gap-3 pt-4">
                <button
                  type="button"
                  onClick={() => setShowCreateModal(false)}
                  className="px-4 py-2 bg-slate-700 hover:bg-slate-600 rounded-lg transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 bg-yellow-500 hover:bg-yellow-600 text-black rounded-lg transition-colors"
                >
                  Submit Request
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default FeatureRequestPage;

