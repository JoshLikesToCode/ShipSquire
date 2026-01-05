import { useState, useEffect, useCallback } from 'react'
import { useParams, Link } from 'react-router-dom'
import { api } from '../services/api'
import type { ServiceResponse, RunbookResponse, RunbookRequest } from '../types'

export default function ServiceDetailPage() {
  const { serviceId } = useParams<{ serviceId: string }>()
  const [service, setService] = useState<ServiceResponse | null>(null)
  const [runbooks, setRunbooks] = useState<RunbookResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showForm, setShowForm] = useState(false)
  const [formData, setFormData] = useState<RunbookRequest>({
    title: '',
    summary: '',
  })

  const loadData = useCallback(async () => {
    if (!serviceId) return
    try {
      setLoading(true)
      const [svcData, rbData] = await Promise.all([
        api.getService(serviceId),
        api.getServiceRunbooks(serviceId),
      ])
      setService(svcData)
      setRunbooks(rbData)
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

  if (loading) return <div className="loading">Loading...</div>
  if (!service) return <div className="error">Service not found</div>

  return (
    <div>
      <div className="page-header">
        <div>
          <h2>{service.name}</h2>
          <p>{service.description}</p>
        </div>
        <button className="btn btn-primary" onClick={() => setShowForm(!showForm)}>
          {showForm ? 'Cancel' : '+ New Runbook'}
        </button>
      </div>

      {error && <div className="error">{error}</div>}

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

      <h3>Runbooks</h3>
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
    </div>
  )
}
