import { Routes, Route, Link, useNavigate } from 'react-router-dom'
import { useState, useEffect } from 'react'
import ServiceListPage from './pages/ServiceListPage'
import ServiceDetailPage from './pages/ServiceDetailPage'
import RunbookEditorPage from './pages/RunbookEditorPage'
import LoginPage from './pages/LoginPage'
import { api } from './services/api'

function App() {
  const navigate = useNavigate()
  const [user, setUser] = useState<any>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    checkAuth()
  }, [])

  const checkAuth = async () => {
    try {
      const currentUser = await api.getCurrentUser()
      setUser(currentUser)
    } catch (err) {
      // Not authenticated - that's okay, we'll show login page
      setUser(null)
    } finally {
      setLoading(false)
    }
  }

  const handleLogout = async () => {
    try {
      await api.logout()
      setUser(null)
      navigate('/login')
    } catch (err) {
      console.error('Logout failed:', err)
    }
  }

  if (loading) {
    return <div className="loading">Loading...</div>
  }

  return (
    <div className="app">
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route
          path="/*"
          element={
            <>
              <nav className="nav">
                <h1>ShipSquire</h1>
                <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
                  <Link to="/services">Services</Link>
                  {user && (
                    <>
                      <span style={{ color: '#ecf0f1', fontSize: '0.9rem' }}>
                        {user.displayName || user.email}
                        {user.authProvider === 'github' && ' (GitHub)'}
                      </span>
                      <button
                        onClick={handleLogout}
                        className="btn btn-secondary"
                        style={{ padding: '0.25rem 0.75rem' }}
                      >
                        Logout
                      </button>
                    </>
                  )}
                </div>
              </nav>
              <main className="main">
                <Routes>
                  <Route path="/" element={<ServiceListPage />} />
                  <Route path="/services" element={<ServiceListPage />} />
                  <Route path="/services/:serviceId" element={<ServiceDetailPage />} />
                  <Route path="/runbooks/:runbookId" element={<RunbookEditorPage />} />
                </Routes>
              </main>
            </>
          }
        />
      </Routes>
    </div>
  )
}

export default App
