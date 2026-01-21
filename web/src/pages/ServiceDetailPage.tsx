import { useState, useEffect, useCallback } from 'react'
import { useParams, Link } from 'react-router-dom'
import { api } from '../services/api'
import type { ServiceResponse, RunbookResponse, RunbookRequest, IncidentResponse, IncidentRequest } from '../types'
import { IncidentSeverity, IncidentStatus } from '../types'
import RepoPickerModal from '../components/RepoPickerModal'
import ConfirmModal from '../components/ConfirmModal'

export default function ServiceDetailPage() {
  const { serviceId } = useParams<{ serviceId: string }>()
  const [service, setService] = useState<ServiceResponse | null>(null)
  const [runbooks, setRunbooks] = useState<RunbookResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showForm, setShowForm] = useState(false)
  const [showRepoModal, setShowRepoModal] = useState(false)
  const [showGenerateModal, setShowGenerateModal] = useState(false)
  const [generating, setGenerating] = useState(false)
  const [generateError, setGenerateError] = useState<string | null>(null)
  const [formData, setFormData] = useState<RunbookRequest>({
    title: '',
    summary: '',
  })
  const [incidents, setIncidents] = useState<IncidentResponse[]>([])
  const [showIncidentForm, setShowIncidentForm] = useState(false)
  const [incidentFormData, setIncidentFormData] = useState<IncidentRequest>({
    title: '',
    severity: IncidentSeverity.Sev3,
    startedAt: new Date().toISOString(),
  })

  const loadData = useCallback(async () => {
    if (!serviceId) return
    try {
      setLoading(true)
      const [svcData, rbData, incData] = await Promise.all([
        api.getService(serviceId),
        api.getServiceRunbooks(serviceId),
        api.getServiceIncidents(serviceId),
      ])
      setService(svcData)
      setRunbooks(rbData)
      setIncidents(incData)
      setError(null)
    } catch (err) {
      setError('Failed to load data')
      console.error(err)
    } finally {
      setLoading(false)
    }
  }, [serviceId])

  useEffect(() => {
    loadData()
  }, [loadData])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!serviceId) return
    try {
      await api.createRunbook(serviceId, formData)
      setFormData({ title: '', summary: '' })
      setShowForm(false)
      loadData()
    } catch (err) {
      setError('Failed to create runbook')
      console.error(err)
    }
  }

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this runbook?')) return
    try {
      await api.deleteRunbook(id)
      loadData()
    } catch (err) {
      setError('Failed to delete runbook')
      console.error(err)
    }
  }

  const handleRepoSelect = async (repo: any) => {
    if (!serviceId) return
    try {
      await api.linkRepoToService(serviceId, {
        provider: 'github',
        owner: repo.owner.login,
        name: repo.name,
        url: repo.html_url,
        defaultBranch: repo.default_branch,
        primaryLanguage: repo.language,
      })
      loadData()
    } catch (err) {
      setError('Failed to link repository')
      console.error(err)
    }
  }

  const handleGenerateClick = () => {
    if (runbooks.length > 0) {
      setShowGenerateModal(true)
    } else {
      handleGenerateConfirm()
    }
  }

  const handleGenerateConfirm = async () => {
    if (!serviceId) return
    setGenerating(true)
    setGenerateError(null)
    try {
      await api.generateRunbook(serviceId)
      setShowGenerateModal(false)
      loadData()
    } catch (err: any) {
      setGenerateError(err.message || 'Failed to generate runbook')
      console.error(err)
    } finally {
      setGenerating(false)
    }
  }

  const handleIncidentSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!serviceId) return
    try {
      await api.createIncident(serviceId, incidentFormData)
      setIncidentFormData({ title: '', severity: IncidentSeverity.Sev3, startedAt: new Date().toISOString() })
      setShowIncidentForm(false)
      loadData()
    } catch (err) {
      setError('Failed to create incident')
      console.error(err)
    }
  }

  const getSeverityClass = (severity: string) => {
    switch (severity) {
      case IncidentSeverity.Sev1: return 'severity-sev1'
      case IncidentSeverity.Sev2: return 'severity-sev2'
      case IncidentSeverity.Sev3: return 'severity-sev3'
      case IncidentSeverity.Sev4: return 'severity-sev4'
      default: return ''
    }
  }

  const getStatusClass = (status: string) => {
    switch (status) {
      case IncidentStatus.Open: return 'status-open'
      case IncidentStatus.Investigating: return 'status-investigating'
      case IncidentStatus.Mitigated: return 'status-mitigated'
      case IncidentStatus.Resolved: return 'status-resolved'
      default: return ''
    }
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString()
  }

  const hasRepoLinked = !!(service?.repo?.owner && service?.repo?.name)

  if (loading) return <div className="loading">Loading...</div>
  if (!service) return <div className="error">Service not found</div>

  return (
    <div>
      <div className="page-header">
        <div>
          <h2>{service.name}</h2>
          <p>{service.description}</p>
          {service.repo ? (
            <div className="repo-info" style={{ marginTop: '0.5rem' }}>
              <strong>Repository:</strong>{' '}
              <a href={service.repo.url} target="_blank" rel="noopener noreferrer" className="link">
                {service.repo.owner}/{service.repo.name}
              </a>
              {service.repo.primaryLanguage && (
                <span style={{ marginLeft: '1rem', color: '#7f8c8d' }}>
                  {service.repo.primaryLanguage}
                </span>
              )}
              <button
                className="btn btn-secondary"
                onClick={() => setShowRepoModal(true)}
                style={{ marginLeft: '1rem', padding: '0.25rem 0.5rem', fontSize: '0.9rem' }}
              >
                Change Repo
              </button>
            </div>
          ) : (
            <button
              className="btn btn-secondary"
              onClick={() => setShowRepoModal(true)}
              style={{ marginTop: '0.5rem' }}
            >
              Connect Repository
            </button>
          )}
        </div>
        <div className="header-actions">
          <button
            className="btn btn-secondary"
            onClick={handleGenerateClick}
            disabled={!hasRepoLinked || generating}
            title={!hasRepoLinked ? 'Connect a repository to generate a draft' : undefined}
          >
            {generating ? 'Generating...' : 'Generate Draft'}
          </button>
          <button className="btn btn-primary" onClick={() => setShowForm(!showForm)}>
            {showForm ? 'Cancel' : '+ New Runbook'}
          </button>
        </div>
      </div>

      {error && <div className="error">{error}</div>}
      {generateError && <div className="error">{generateError}</div>}

      {showForm && (
        <form className="form" onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Title</label>
            <input
              required
              value={formData.title}
              onChange={(e) => setFormData({ ...formData, title: e.target.value })}
            />
          </div>
          <div className="form-group">
            <label>Summary</label>
            <textarea
              value={formData.summary}
              onChange={(e) => setFormData({ ...formData, summary: e.target.value })}
            />
          </div>
          <div className="form-actions">
            <button type="button" className="btn btn-secondary" onClick={() => setShowForm(false)}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary">
              Create
            </button>
          </div>
        </form>
      )}

      <div className="section-header">
        <h3>Incidents</h3>
        <button className="btn btn-primary" onClick={() => setShowIncidentForm(!showIncidentForm)}>
          {showIncidentForm ? 'Cancel' : '+ New Incident'}
        </button>
      </div>

      {showIncidentForm && (
        <form className="form" onSubmit={handleIncidentSubmit}>
          <div className="form-group">
            <label>Title</label>
            <input
              required
              value={incidentFormData.title}
              onChange={(e) => setIncidentFormData({ ...incidentFormData, title: e.target.value })}
              placeholder="Brief description of the incident"
            />
          </div>
          <div className="form-group">
            <label>Severity</label>
            <select
              value={incidentFormData.severity}
              onChange={(e) => setIncidentFormData({ ...incidentFormData, severity: e.target.value })}
            >
              <option value={IncidentSeverity.Sev1}>SEV1 - Critical</option>
              <option value={IncidentSeverity.Sev2}>SEV2 - Major</option>
              <option value={IncidentSeverity.Sev3}>SEV3 - Minor</option>
              <option value={IncidentSeverity.Sev4}>SEV4 - Low</option>
            </select>
          </div>
          <div className="form-actions">
            <button type="button" className="btn btn-secondary" onClick={() => setShowIncidentForm(false)}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary">
              Create Incident
            </button>
          </div>
        </form>
      )}

      <div className="incident-list">
        {incidents.length === 0 ? (
          <p>No incidents recorded.</p>
        ) : (
          incidents.map((incident) => (
            <div key={incident.id} className="card incident-card">
              <div className="incident-header">
                <Link to={`/incidents/${incident.id}`} className="link incident-title">
                  {incident.title}
                </Link>
                <div className="incident-badges">
                  <span className={`badge ${getSeverityClass(incident.severity)}`}>
                    {incident.severity.toUpperCase()}
                  </span>
                  <span className={`badge ${getStatusClass(incident.status)}`}>
                    {incident.status}
                  </span>
                </div>
              </div>
              <div className="incident-meta">
                <span>Started: {formatDate(incident.startedAt)}</span>
                {incident.runbookTitle && (
                  <span className="incident-runbook">
                    Runbook: <Link to={`/runbooks/${incident.runbookId}`} className="link">{incident.runbookTitle}</Link>
                  </span>
                )}
              </div>
            </div>
          ))
        )}
      </div>

      <div className="section-header" style={{ marginTop: '2rem' }}>
        <h3>Runbooks</h3>
      </div>
      <div>
        {runbooks.length === 0 ? (
          <p>No runbooks yet. Create one to get started!</p>
        ) : (
          runbooks.map((runbook) => (
            <div key={runbook.id} className="card">
              <h3>
                <Link to={`/runbooks/${runbook.id}`} className="link">
                  {runbook.title}
                </Link>
                {runbook.origin && (
                  <span className={`badge badge-${runbook.origin}`}>
                    {runbook.origin === 'generated' ? 'Generated' : 'Manual'}
                  </span>
                )}
              </h3>
              <p>Status: {runbook.status} | Version: {runbook.version}</p>
              {runbook.summary && <p>{runbook.summary}</p>}
              <button className="btn btn-danger" onClick={() => handleDelete(runbook.id)}>
                Delete
              </button>
            </div>
          ))
        )}
      </div>

      <RepoPickerModal
        isOpen={showRepoModal}
        onClose={() => setShowRepoModal(false)}
        onSelect={handleRepoSelect}
      />

      <ConfirmModal
        isOpen={showGenerateModal}
        onClose={() => setShowGenerateModal(false)}
        onConfirm={handleGenerateConfirm}
        title="Generate New Runbook"
        message="This service already has runbooks. Generating a new draft will create an additional runbook. Do you want to continue?"
        loading={generating}
        confirmText="Generate"
        cancelText="Cancel"
      />
    </div>
  )
}
