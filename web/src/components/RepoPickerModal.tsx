import { useState, useEffect } from 'react'
import { api } from '../services/api'

interface Repo {
  id: number
  name: string
  full_name: string
  html_url: string
  description: string | null
  private: boolean
  owner: {
    login: string
  }
  language: string | null
  default_branch: string
  updated_at: string
}

interface RepoPickerModalProps {
  isOpen: boolean
  onClose: () => void
  onSelect: (repo: Repo) => void
}

export default function RepoPickerModal({ isOpen, onClose, onSelect }: RepoPickerModalProps) {
  const [repos, setRepos] = useState<Repo[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [searchTerm, setSearchTerm] = useState('')

  useEffect(() => {
    if (isOpen) {
      loadRepos()
    }
  }, [isOpen])

  const loadRepos = async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await api.getGitHubRepos(1, 100) // Load first 100 repos
      setRepos(data)
    } catch (err: any) {
      setError(err.message || 'Failed to load repositories')
    } finally {
      setLoading(false)
    }
  }

  const filteredRepos = repos.filter((repo) =>
    repo.full_name.toLowerCase().includes(searchTerm.toLowerCase()) ||
    (repo.description && repo.description.toLowerCase().includes(searchTerm.toLowerCase()))
  )

  if (!isOpen) return null

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Connect GitHub Repository</h2>
          <button onClick={onClose} className="close-button">&times;</button>
        </div>

        <div className="modal-body">
          {error && (
            <div className="error-message">
              {error}
              {error.includes('GitHub account not linked') && (
                <p style={{ marginTop: '0.5rem' }}>
                  Please <a href="/login">log in with GitHub</a> to connect repositories.
                </p>
              )}
            </div>
          )}

          {!error && (
            <>
              <input
                type="text"
                className="search-input"
                placeholder="Search repositories..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                autoFocus
              />

              {loading ? (
                <div className="loading">Loading repositories...</div>
              ) : (
                <div className="repo-list">
                  {filteredRepos.length === 0 ? (
                    <p className="no-results">No repositories found</p>
                  ) : (
                    filteredRepos.map((repo) => (
                      <div
                        key={repo.id}
                        className="repo-item"
                        onClick={() => {
                          onSelect(repo)
                          onClose()
                        }}
                      >
                        <div className="repo-info">
                          <div className="repo-name">
                            {repo.full_name}
                            {repo.private && <span className="badge">Private</span>}
                          </div>
                          {repo.description && (
                            <div className="repo-description">{repo.description}</div>
                          )}
                          <div className="repo-meta">
                            {repo.language && <span className="language">{repo.language}</span>}
                            <span className="branch">Default: {repo.default_branch}</span>
                            <span className="updated">
                              Updated {new Date(repo.updated_at).toLocaleDateString()}
                            </span>
                          </div>
                        </div>
                      </div>
                    ))
                  )}
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  )
}
