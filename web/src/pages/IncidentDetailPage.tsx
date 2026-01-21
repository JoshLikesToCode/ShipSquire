import { useState, useEffect, useCallback } from 'react'
import { useParams, Link } from 'react-router-dom'
import { api } from '../services/api'
import type { IncidentResponse, TimelineEntryResponse, TimelineEntryRequest, RunbookResponse } from '../types'
import { IncidentStatus, IncidentSeverity, TimelineEntryType } from '../types'

export default function IncidentDetailPage() {
  const { incidentId } = useParams<{ incidentId: string }>()
  const [incident, setIncident] = useState<IncidentResponse | null>(null)
  const [timeline, setTimeline] = useState<TimelineEntryResponse[]>([])
  const [runbook, setRunbook] = useState<RunbookResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [entryFormData, setEntryFormData] = useState<TimelineEntryRequest>({
    entryType: TimelineEntryType.Note,
    bodyMarkdown: '',
  })
  const [submitting, setSubmitting] = useState(false)
  const [statusUpdating, setStatusUpdating] = useState(false)

  const loadData = useCallback(async () => {
    if (!incidentId) return
    try {
      setLoading(true)
      const [incData, timelineData] = await Promise.all([
        api.getIncident(incidentId),
        api.getIncidentTimeline(incidentId),
      ])
      setIncident(incData)
      setTimeline(timelineData)

      // Load runbook if attached
      if (incData.runbookId) {
        try {
          const rbData = await api.getRunbook(incData.runbookId)
          setRunbook(rbData)
        } catch {
          // Runbook may have been deleted
          setRunbook(null)
        }
      }
      setError(null)
    } catch (err) {
      setError('Failed to load incident')
      console.error(err)
    } finally {
      setLoading(false)
    }
  }, [incidentId])

  useEffect(() => {
    loadData()
  }, [loadData])

  const handleAddEntry = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!incidentId || !entryFormData.bodyMarkdown.trim()) return
    try {
      setSubmitting(true)
      await api.addTimelineEntry(incidentId, entryFormData)
      setEntryFormData({ entryType: TimelineEntryType.Note, bodyMarkdown: '' })
      loadData()
    } catch (err) {
      setError('Failed to add timeline entry')
      console.error(err)
    } finally {
      setSubmitting(false)
    }
  }

  const handleStatusChange = async (newStatus: string) => {
    if (!incidentId || !incident) return
    try {
      setStatusUpdating(true)
      const updates: any = { status: newStatus }
      if (newStatus === IncidentStatus.Resolved && !incident.endedAt) {
        updates.endedAt = new Date().toISOString()
      }
      await api.updateIncident(incidentId, updates)
      loadData()
    } catch (err) {
      setError('Failed to update status')
      console.error(err)
    } finally {
      setStatusUpdating(false)
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

  const getEntryTypeIcon = (entryType: string) => {
    switch (entryType) {
      case TimelineEntryType.Note: return 'N'
      case TimelineEntryType.Action: return 'A'
      case TimelineEntryType.Decision: return 'D'
      case TimelineEntryType.Observation: return 'O'
      default: return '?'
    }
  }

  const formatTime = (dateString: string) => {
    return new Date(dateString).toLocaleString()
  }

  const getNextStatusOptions = (currentStatus: string) => {
    switch (currentStatus) {
      case IncidentStatus.Open:
        return [IncidentStatus.Investigating]
      case IncidentStatus.Investigating:
        return [IncidentStatus.Mitigated, IncidentStatus.Resolved]
      case IncidentStatus.Mitigated:
        return [IncidentStatus.Resolved, IncidentStatus.Investigating]
      case IncidentStatus.Resolved:
        return [IncidentStatus.Open]
      default:
        return []
    }
  }

  if (loading) return <div className="loading">Loading...</div>
  if (!incident) return <div className="error">Incident not found</div>

  const nextStatuses = getNextStatusOptions(incident.status)

  return (
    <div className="incident-detail">
      {/* Header */}
      <div className="incident-detail-header">
        <div className="incident-detail-title">
          <Link to={`/services/${incident.serviceId}`} className="link back-link">
            Back to Service
          </Link>
          <h2>{incident.title}</h2>
          <div className="incident-badges">
            <span className={`badge ${getSeverityClass(incident.severity)}`}>
              {incident.severity.toUpperCase()}
            </span>
            <span className={`badge ${getStatusClass(incident.status)}`}>
              {incident.status}
            </span>
          </div>
        </div>

        {/* Status actions */}
        <div className="status-actions">
          {nextStatuses.map((status) => (
            <button
              key={status}
              className={`btn btn-status ${getStatusClass(status)}`}
              onClick={() => handleStatusChange(status)}
              disabled={statusUpdating}
            >
              Mark {status}
            </button>
          ))}
        </div>
      </div>

      {error && <div className="error">{error}</div>}

      {/* Incident meta */}
      <div className="incident-meta-detail">
        <div>Started: {formatTime(incident.startedAt)}</div>
        {incident.endedAt && <div>Ended: {formatTime(incident.endedAt)}</div>}
      </div>

      {/* Quick links to runbook sections */}
      {runbook && runbook.sections && runbook.sections.length > 0 && (
        <div className="runbook-quick-links">
          <h4>Runbook: {runbook.title}</h4>
          <div className="quick-links">
            <Link to={`/runbooks/${runbook.id}`} className="btn btn-secondary quick-link">
              Open Full Runbook
            </Link>
            {runbook.sections.map((section) => (
              <Link
                key={section.id}
                to={`/runbooks/${runbook.id}#section-${section.key}`}
                className="btn btn-secondary quick-link"
              >
                {section.title}
              </Link>
            ))}
          </div>
        </div>
      )}

      {/* Timeline Entry Form - at top for quick access during incident */}
      <div className="timeline-entry-form">
        <h4>Add Timeline Entry</h4>
        <form onSubmit={handleAddEntry}>
          <div className="form-row">
            <select
              value={entryFormData.entryType}
              onChange={(e) => setEntryFormData({ ...entryFormData, entryType: e.target.value })}
              className="entry-type-select"
            >
              <option value={TimelineEntryType.Note}>Note</option>
              <option value={TimelineEntryType.Action}>Action</option>
              <option value={TimelineEntryType.Decision}>Decision</option>
              <option value={TimelineEntryType.Observation}>Observation</option>
            </select>
            <input
              type="text"
              value={entryFormData.bodyMarkdown}
              onChange={(e) => setEntryFormData({ ...entryFormData, bodyMarkdown: e.target.value })}
              placeholder="What happened?"
              className="entry-body-input"
              required
            />
            <button type="submit" className="btn btn-primary" disabled={submitting || !entryFormData.bodyMarkdown.trim()}>
              {submitting ? 'Adding...' : 'Add'}
            </button>
          </div>
        </form>
      </div>

      {/* Timeline Feed */}
      <div className="timeline-section">
        <h4>Timeline</h4>
        {timeline.length === 0 ? (
          <p className="no-entries">No timeline entries yet. Add one above to start documenting.</p>
        ) : (
          <div className="timeline-feed">
            {timeline.map((entry) => (
              <div key={entry.id} className={`timeline-entry entry-type-${entry.entryType}`}>
                <div className="timeline-entry-icon">
                  <span className={`entry-icon ${entry.entryType}`}>{getEntryTypeIcon(entry.entryType)}</span>
                </div>
                <div className="timeline-entry-content">
                  <div className="timeline-entry-header">
                    <span className="entry-type-label">{entry.entryType}</span>
                    <span className="entry-time">{formatTime(entry.occurredAt)}</span>
                  </div>
                  <div className="timeline-entry-body">{entry.bodyMarkdown}</div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
