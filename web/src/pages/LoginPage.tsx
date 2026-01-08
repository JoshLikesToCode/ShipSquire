import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'

export default function LoginPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    // Check if there's an error from OAuth callback
    const errorParam = searchParams.get('error')
    if (errorParam) {
      setError(decodeURIComponent(errorParam))
    }
  }, [searchParams])

  const handleGitHubLogin = () => {
    const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'
    window.location.href = `${apiBaseUrl}/auth/github/login`
  }

  const handleDevLogin = () => {
    // For development: just navigate to services (will use X-User-Email header)
    navigate('/services')
  }

  return (
    <div style={{
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      justifyContent: 'center',
      minHeight: '100vh',
      gap: '2rem'
    }}>
      <div style={{ textAlign: 'center' }}>
        <h1 style={{ fontSize: '3rem', marginBottom: '0.5rem' }}>ShipSquire</h1>
        <p style={{ color: '#7f8c8d', fontSize: '1.2rem' }}>
          Developer Operations Portal
        </p>
      </div>

      {error && (
        <div className="error" style={{ maxWidth: '400px' }}>
          <strong>Authentication Error:</strong> {error}
        </div>
      )}

      <div style={{
        display: 'flex',
        flexDirection: 'column',
        gap: '1rem',
        width: '300px'
      }}>
        <button
          onClick={handleGitHubLogin}
          className="btn btn-primary"
          style={{
            padding: '1rem',
            fontSize: '1.1rem',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            gap: '0.5rem'
          }}
        >
          <svg height="20" width="20" viewBox="0 0 16 16" fill="currentColor">
            <path d="M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2.01.37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82.64-.18 1.32-.27 2-.27.68 0 1.36.09 2 .27 1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.013 8.013 0 0016 8c0-4.42-3.58-8-8-8z"/>
          </svg>
          Login with GitHub
        </button>

        <div style={{
          textAlign: 'center',
          color: '#7f8c8d',
          fontSize: '0.9rem'
        }}>
          — or —
        </div>

        <button
          onClick={handleDevLogin}
          className="btn btn-secondary"
          style={{ padding: '1rem', fontSize: '1.1rem' }}
        >
          Continue as Dev User
        </button>

        <p style={{
          fontSize: '0.85rem',
          color: '#95a5a6',
          textAlign: 'center',
          marginTop: '1rem'
        }}>
          Dev mode uses X-User-Email header for testing
        </p>
      </div>
    </div>
  )
}
