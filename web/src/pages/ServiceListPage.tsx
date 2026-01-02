import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../services/api'
import type { ServiceResponse, ServiceRequest } from '../types'

export default function ServiceListPage() {
  const [services, setServices] = useState<ServiceResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showForm, setShowForm] = useState(false)
  const [formData, setFormData] = useState<ServiceRequest>({
    name: '',
    slug: '',
    description: '',
  })

  useEffect(() => {
    loadServices()
  }, [])

  const loadServices = async () => {
    try {
      setLoading(true)
      const data = await api.getServices()
      setServices(data)
      setError(null)
    } catch (err) {
      setError('Failed to load services')
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      await api.createService(formData)
      setFormData({ name: '', slug: '', description: '' })
      setShowForm(false)
      loadServices()
    } catch (err) {
      setError('Failed to create service')
      console.error(err)
    }
  }

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this service?')) return
    try {
      await api.deleteService(id)
      loadServices()
    } catch (err) {
      setError('Failed to delete service')
      console.error(err)
    }
  }

  if (loading) return <div className="loading">Loading...</div>

  return (
    <div>
      <div className="page-header">
        <h2>Services</h2>
        <button className="btn btn-primary" onClick={() => setShowForm(!showForm)}>
          {showForm ? 'Cancel' : '+ New Service'}
        </button>
      </div>

      {error && <div className="error">{error}</div>}

      {showForm && (
        <form className="form" onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Name</label>
            <input
              required
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            />
          </div>
          <div className="form-group">
            <label>Slug</label>
            <input
              required
              value={formData.slug}
              onChange={(e) => setFormData({ ...formData, slug: e.target.value })}
            />
          </div>
          <div className="form-group">
            <label>Description</label>
            <textarea
              value={formData.description}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
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

      <div>
        {services.length === 0 ? (
          <p>No services yet. Create one to get started!</p>
        ) : (
          services.map((service) => (
            <div key={service.id} className="card">
              <h3>
                <Link to={`/services/${service.id}`} className="link">
                  {service.name}
                </Link>
              </h3>
              <p>{service.slug}</p>
              {service.description && <p>{service.description}</p>}
              <button className="btn btn-danger" onClick={() => handleDelete(service.id)}>
                Delete
              </button>
            </div>
          ))
        )}
      </div>
    </div>
  )
}
