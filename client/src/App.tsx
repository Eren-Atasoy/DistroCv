import { Routes, Route } from 'react-router-dom'
import LandingPage from './pages/LandingPage'
import Dashboard from './pages/Dashboard'
import SwipeInterface from './pages/SwipeInterface'
import ResumeEditor from './pages/ResumeEditor'
import ApplicationsPage from './pages/ApplicationsPage'
import InterviewPrep from './pages/InterviewPrep'
import AdminCompanies from './pages/AdminCompanies'
import SkillGapPage from './pages/SkillGapPage'
import LinkedInOptimizerPage from './pages/LinkedInOptimizerPage'
import PreferencesPage from './pages/PreferencesPage'
import BugReportPage from './pages/BugReportPage'
import FeatureRequestPage from './pages/FeatureRequestPage'
import Layout from './components/layout/Layout'
import AdminLayout from './components/layout/AdminLayout'
import { ProtectedRoute } from './components/auth/ProtectedRoute'
import { AdminRoute } from './components/auth/AdminRoute'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import AdminDashboard from './pages/admin/AdminDashboard'
import AdminSeedPage from './pages/admin/AdminSeedPage'

function App() {
  return (
    <Routes>
      {/* Public Routes */}
      <Route path="/" element={<LandingPage />} />

      {/* Auth Routes */}
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      {/* Protected User Routes */}
      <Route element={<ProtectedRoute />}>
        <Route element={<Layout />}>
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/discover" element={<SwipeInterface />} />
          <Route path="/applications" element={<ApplicationsPage />} />
          <Route path="/applications/:id/resume" element={<ResumeEditor />} />
          <Route path="/applications/:id/interview" element={<InterviewPrep />} />
          <Route path="/skills" element={<SkillGapPage />} />
          <Route path="/linkedin-optimizer" element={<LinkedInOptimizerPage />} />
          <Route path="/preferences" element={<PreferencesPage />} />
          <Route path="/bugs" element={<BugReportPage />} />
          <Route path="/features" element={<FeatureRequestPage />} />
        </Route>
      </Route>

      {/* Admin Routes */}
      <Route element={<AdminRoute />}>
        <Route element={<AdminLayout />}>
          <Route path="/admin" element={<AdminDashboard />} />
          <Route path="/admin/companies" element={<AdminCompanies />} />
          <Route path="/admin/seed" element={<AdminSeedPage />} />
        </Route>
      </Route>
    </Routes>
  )
}

export default App
