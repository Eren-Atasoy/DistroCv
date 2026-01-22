import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import {
  BugAntIcon,
  PlusIcon,
  FunnelIcon,
  CheckCircleIcon,
  ClockIcon,
  ExclamationTriangleIcon,
  HandThumbUpIcon,
  ChatBubbleLeftIcon,
} from '@heroicons/react/24/outline';

interface BugReport {
  id: string;
  title: string;
  description: string;
  stepsToReproduce?: string;
  severity: 'Critical' | 'High' | 'Medium' | 'Low' | 'Trivial';
  priority: 'P0' | 'P1' | 'P2' | 'P3' | 'P4';
  category: string;
  status: 'New' | 'Confirmed' | 'InProgress' | 'Testing' | 'Resolved' | 'Closed';
  reporterName?: string;
  createdAt: string;
  resolvedAt?: string;
  voteCount: number;
  commentCount: number;
  screenshotUrl?: string;
}

const BugReportPage: React.FC = () => {
  const { t } = useTranslation();
  const [bugs, setBugs] = useState<BugReport[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [selectedStatus, setSelectedStatus] = useState<string>('all');
  const [selectedPriority, setSelectedPriority] = useState<string>('all');

  const [newBug, setNewBug] = useState({
    title: '',
    description: '',
    stepsToReproduce: '',
    expectedBehavior: '',
    actualBehavior: '',
    severity: 'Medium' as const,
    category: 'Other',
    pageUrl: window.location.href,
    browser: navigator.userAgent,
    screenshotUrl: '',
  });

  const categories = [
    'Authentication', 'ResumeUpload', 'JobMatching', 'ApplicationSending',
    'InterviewPrep', 'Dashboard', 'ProfileManagement', 'Notifications',
    'Performance', 'UI_UX', 'Localization', 'Integration', 'Security', 'Other'
  ];

  const severities = ['Critical', 'High', 'Medium', 'Low', 'Trivial'];
  const statuses = ['New', 'Confirmed', 'InProgress', 'Testing', 'Resolved', 'Closed'];

  useEffect(() => {
    fetchBugs();
  }, [selectedStatus, selectedPriority]);

  const fetchBugs = async () => {
    try {
      setIsLoading(true);
      const params = new URLSearchParams();
      if (selectedStatus !== 'all') params.append('status', selectedStatus);
      if (selectedPriority !== 'all') params.append('priority', selectedPriority);

      const response = await fetch(`/api/betatesting/bugs?${params}`, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
        },
      });

      if (response.ok) {
        const data = await response.json();
        setBugs(data);
      }
    } catch (error) {
      console.error('Error fetching bugs:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleSubmitBug = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const response = await fetch('/api/betatesting/bugs', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
        },
        body: JSON.stringify(newBug),
      });

      if (response.ok) {
        setShowCreateModal(false);
        setNewBug({
          title: '',
          description: '',
          stepsToReproduce: '',
          expectedBehavior: '',
          actualBehavior: '',
          severity: 'Medium',
          category: 'Other',
          pageUrl: window.location.href,
          browser: navigator.userAgent,
          screenshotUrl: '',
        });
        fetchBugs();
      }
    } catch (error) {
      console.error('Error submitting bug:', error);
    }
  };

  const handleVote = async (bugId: string) => {
    try {
      await fetch(`/api/betatesting/bugs/${bugId}/vote`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
        },
      });
      fetchBugs();
    } catch (error) {
      console.error('Error voting:', error);
    }
  };

  const getSeverityColor = (severity: string) => {
    switch (severity) {
      case 'Critical': return 'bg-red-100 text-red-800 border-red-200';
      case 'High': return 'bg-orange-100 text-orange-800 border-orange-200';
      case 'Medium': return 'bg-yellow-100 text-yellow-800 border-yellow-200';
      case 'Low': return 'bg-blue-100 text-blue-800 border-blue-200';
      default: return 'bg-gray-100 text-gray-800 border-gray-200';
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'New': return <ClockIcon className="w-4 h-4" />;
      case 'InProgress': return <ClockIcon className="w-4 h-4 text-blue-500" />;
      case 'Resolved': return <CheckCircleIcon className="w-4 h-4 text-green-500" />;
      default: return <ClockIcon className="w-4 h-4" />;
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 text-white p-6">
      {/* Header */}
      <div className="flex items-center justify-between mb-8">
        <div className="flex items-center gap-3">
          <div className="p-3 bg-red-500/20 rounded-xl">
            <BugAntIcon className="w-8 h-8 text-red-400" />
          </div>
          <div>
            <h1 className="text-2xl font-bold">Bug Reports</h1>
            <p className="text-slate-400">Help us improve by reporting issues</p>
          </div>
        </div>
        <button
          onClick={() => setShowCreateModal(true)}
          className="flex items-center gap-2 px-4 py-2 bg-red-500 hover:bg-red-600 rounded-lg transition-colors"
        >
          <PlusIcon className="w-5 h-5" />
          Report Bug
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
          value={selectedPriority}
          onChange={(e) => setSelectedPriority(e.target.value)}
          className="bg-slate-700 border border-slate-600 rounded-lg px-3 py-2 text-sm"
        >
          <option value="all">All Priority</option>
          <option value="P0">P0 - Critical</option>
          <option value="P1">P1 - High</option>
          <option value="P2">P2 - Medium</option>
          <option value="P3">P3 - Low</option>
        </select>
      </div>

      {/* Bug List */}
      {isLoading ? (
        <div className="flex justify-center py-12">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-red-500"></div>
        </div>
      ) : bugs.length === 0 ? (
        <div className="text-center py-12">
          <BugAntIcon className="w-16 h-16 text-slate-600 mx-auto mb-4" />
          <p className="text-slate-400">No bug reports yet. Everything looks good!</p>
        </div>
      ) : (
        <div className="space-y-4">
          {bugs.map((bug) => (
            <div
              key={bug.id}
              className="bg-slate-800/50 border border-slate-700 rounded-xl p-6 hover:border-slate-600 transition-colors"
            >
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <div className="flex items-center gap-3 mb-2">
                    {getStatusIcon(bug.status)}
                    <h3 className="text-lg font-semibold">{bug.title}</h3>
                    <span className={`text-xs px-2 py-1 rounded-full border ${getSeverityColor(bug.severity)}`}>
                      {bug.severity}
                    </span>
                    <span className="text-xs px-2 py-1 bg-slate-700 rounded-full">
                      {bug.priority}
                    </span>
                  </div>
                  <p className="text-slate-400 text-sm mb-3 line-clamp-2">{bug.description}</p>
                  <div className="flex items-center gap-4 text-sm text-slate-500">
                    <span>{bug.category}</span>
                    <span>•</span>
                    <span>{new Date(bug.createdAt).toLocaleDateString()}</span>
                    {bug.reporterName && (
                      <>
                        <span>•</span>
                        <span>by {bug.reporterName}</span>
                      </>
                    )}
                  </div>
                </div>
                <div className="flex items-center gap-4">
                  <button
                    onClick={() => handleVote(bug.id)}
                    className="flex items-center gap-1 text-slate-400 hover:text-green-400 transition-colors"
                  >
                    <HandThumbUpIcon className="w-5 h-5" />
                    <span>{bug.voteCount}</span>
                  </button>
                  <div className="flex items-center gap-1 text-slate-400">
                    <ChatBubbleLeftIcon className="w-5 h-5" />
                    <span>{bug.commentCount}</span>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Create Bug Modal */}
      {showCreateModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-slate-800 rounded-2xl p-6 max-w-2xl w-full mx-4 max-h-[90vh] overflow-y-auto">
            <h2 className="text-xl font-bold mb-4 flex items-center gap-2">
              <BugAntIcon className="w-6 h-6 text-red-400" />
              Report a Bug
            </h2>
            <form onSubmit={handleSubmitBug} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-1">Title *</label>
                <input
                  type="text"
                  required
                  value={newBug.title}
                  onChange={(e) => setNewBug({ ...newBug, title: e.target.value })}
                  className="w-full bg-slate-700 border border-slate-600 rounded-lg px-4 py-2 focus:outline-none focus:border-red-500"
                  placeholder="Brief description of the bug"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-300 mb-1">Description *</label>
                <textarea
                  required
                  rows={3}
                  value={newBug.description}
                  onChange={(e) => setNewBug({ ...newBug, description: e.target.value })}
                  className="w-full bg-slate-700 border border-slate-600 rounded-lg px-4 py-2 focus:outline-none focus:border-red-500"
                  placeholder="Detailed description of what went wrong"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-300 mb-1">Steps to Reproduce</label>
                <textarea
                  rows={3}
                  value={newBug.stepsToReproduce}
                  onChange={(e) => setNewBug({ ...newBug, stepsToReproduce: e.target.value })}
                  className="w-full bg-slate-700 border border-slate-600 rounded-lg px-4 py-2 focus:outline-none focus:border-red-500"
                  placeholder="1. Go to...&#10;2. Click on...&#10;3. See error..."
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-slate-300 mb-1">Expected Behavior</label>
                  <textarea
                    rows={2}
                    value={newBug.expectedBehavior}
                    onChange={(e) => setNewBug({ ...newBug, expectedBehavior: e.target.value })}
                    className="w-full bg-slate-700 border border-slate-600 rounded-lg px-4 py-2 focus:outline-none focus:border-red-500"
                    placeholder="What should happen"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-300 mb-1">Actual Behavior</label>
                  <textarea
                    rows={2}
                    value={newBug.actualBehavior}
                    onChange={(e) => setNewBug({ ...newBug, actualBehavior: e.target.value })}
                    className="w-full bg-slate-700 border border-slate-600 rounded-lg px-4 py-2 focus:outline-none focus:border-red-500"
                    placeholder="What actually happened"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-slate-300 mb-1">Severity</label>
                  <select
                    value={newBug.severity}
                    onChange={(e) => setNewBug({ ...newBug, severity: e.target.value as any })}
                    className="w-full bg-slate-700 border border-slate-600 rounded-lg px-4 py-2"
                  >
                    {severities.map(severity => (
                      <option key={severity} value={severity}>{severity}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-300 mb-1">Category</label>
                  <select
                    value={newBug.category}
                    onChange={(e) => setNewBug({ ...newBug, category: e.target.value })}
                    className="w-full bg-slate-700 border border-slate-600 rounded-lg px-4 py-2"
                  >
                    {categories.map(category => (
                      <option key={category} value={category}>{category.replace('_', '/')}</option>
                    ))}
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-300 mb-1">Screenshot URL (optional)</label>
                <input
                  type="url"
                  value={newBug.screenshotUrl}
                  onChange={(e) => setNewBug({ ...newBug, screenshotUrl: e.target.value })}
                  className="w-full bg-slate-700 border border-slate-600 rounded-lg px-4 py-2 focus:outline-none focus:border-red-500"
                  placeholder="https://imgur.com/..."
                />
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
                  className="px-4 py-2 bg-red-500 hover:bg-red-600 rounded-lg transition-colors"
                >
                  Submit Bug Report
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default BugReportPage;

