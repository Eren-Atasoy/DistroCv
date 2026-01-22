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
import Layout from './components/layout/Layout'

function App() {
  return (
    <Routes>
      {/* Public Routes */}
      <Route path="/" element={<LandingPage />} />

      {/* Protected Routes */}
      <Route element={<Layout />}>
        <Route path="/dashboard" element={<Dashboard />} />
        <Route path="/discover" element={<SwipeInterface />} />
        <Route path="/applications" element={<ApplicationsPage />} />
        <Route path="/applications/:id/resume" element={<ResumeEditor />} />
        <Route path="/applications/:id/interview" element={<InterviewPrep />} />
        <Route path="/admin/companies" element={<AdminCompanies />} />
        <Route path="/skills" element={<SkillGapPage />} />
        <Route path="/linkedin-optimizer" element={<LinkedInOptimizerPage />} />
        <Route path="/preferences" element={<PreferencesPage />} />
      </Route>
    </Routes>
  )
}

export default App
