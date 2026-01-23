import { useState, useEffect, useCallback } from 'react'
import { useParams, Link } from 'react-router-dom'
import { api } from '../services/api'
import type { IncidentResponse, TimelineEntryResponse, TimelineEntryRequest, RunbookResponse, PostmortemResponse } from '../types'
import { IncidentStatus, IncidentSeverity, TimelineEntryType, ValidStatusTransitions } from '../types'

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
  const [postmortem, setPostmortem] = useState<PostmortemResponse | null>(null)
  const [postmortemLoading, setPostmortemLoading] = useState(false)
  const [showPostmortem, setShowPostmortem] = useState(false)
  const [exporting, setExporting] = useState(false)

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
      setError(null)
      await api.transitionIncidentStatus(incidentId, newStatus)
      loadData()
    } catch (err: any) {
      // Try to parse API error response for better messages
      let message = 'Failed to update status'
      if (err?.message) {
        // Check if it's a JSON error response
        try {
          const errorMatch = err.message.match(/API error: \d+ (.+)/)
          if (errorMatch) {
            message = `Status change failed: ${errorMatch[1]}`
          } else {
            message = err.message
          }
        } catch {
          message = err.message
        }
      }
      setError(message)
      console.error(err)
    } finally {
      setStatusUpdating(false)
    }
  }

  const loadPostmortem = async () => {
    if (!incidentId) return
    try {
      setPostmortemLoading(true)
      const pmData = await api.getPostmortem(incidentId)
      setPostmortem(pmData)
      setShowPostmortem(true)
    } catch (err) {
      // Postmortem may not exist yet
      setPostmortem(null)
    } finally {
      setPostmortemLoading(false)
    }
  }

  const handleExport = async () => {
    if (!incidentId) return
    try {
      setExporting(true)
      setError(null)
      const { content, filename } = await api.exportIncident(incidentId)

      // Create and trigger download
      const blob = new Blob([content], { type: 'text/markdown' })
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = filename
      document.body.appendChild(a)
      a.click()
      document.body.removeChild(a)
      URL.revokeObjectURL(url)
    } catch (err: any) {
      const message = err?.message || 'Failed to export incident'
      setError(message)
      console.error(err)
    } finally {
      setExporting(false)
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
    return ValidStatusTransitions[currentStatus] || []
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

        {/* Header actions */}
        <div className="header-actions">
          <button
            className="btn btn-secondary"
            onClick={handleExport}
            disabled={exporting}
            title="Export incident as Markdown"
          >
            {exporting ? 'Exporting...' : 'Export'}
          </button>
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

      {/* Postmortem Section - only for resolved incidents */}
      {incident.status === IncidentStatus.Resolved && (
        <div className="postmortem-section">
          <div className="section-header">
            <h4>Postmortem</h4>
            {!showPostmortem && (
              <button
                className="btn btn-secondary"
                onClick={loadPostmortem}
                disabled={postmortemLoading}
              >
                {postmortemLoading ? 'Loading...' : 'View Postmortem'}
              </button>
            )}
          </div>

          {showPostmortem && postmortem && (
            <div className="postmortem-content">
              <div className="postmortem-card">
                <h5>Impact</h5>
                <div className="postmortem-markdown">{postmortem.impactMarkdown}</div>
              </div>

              <div className="postmortem-card">
                <h5>Root Cause Analysis</h5>
                <div className="postmortem-markdown">{postmortem.rootCauseMarkdown}</div>
              </div>

              <div className="postmortem-card">
                <h5>Detection</h5>
                <div className="postmortem-markdown">{postmortem.detectionMarkdown}</div>
              </div>

              <div className="postmortem-card">
                <h5>Resolution</h5>
                <div className="postmortem-markdown">{postmortem.resolutionMarkdown}</div>
              </div>

              <div className="postmortem-card">
                <h5>Action Items</h5>
                <div className="postmortem-markdown">{postmortem.actionItemsMarkdown}</div>
              </div>

              <p className="postmortem-note">
                Generated {new Date(postmortem.createdAt).toLocaleDateString()}
                {postmortem.updatedAt !== postmortem.createdAt &&
                  ` · Last updated ${new Date(postmortem.updatedAt).toLocaleDateString()}`
                }
              </p>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
