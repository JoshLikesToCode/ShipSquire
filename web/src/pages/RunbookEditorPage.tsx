import { useState, useEffect, useCallback } from 'react'
import { useParams } from 'react-router-dom'
import { api } from '../services/api'
import type { RunbookResponse, SectionResponse, SectionRequest } from '../types'

export default function RunbookEditorPage() {
  const { runbookId } = useParams<{ runbookId: string }>()
  const [runbook, setRunbook] = useState<RunbookResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [editingSection, setEditingSection] = useState<string | null>(null)
  const [sectionData, setSectionData] = useState<Record<string, string>>({})

  const loadRunbook = useCallback(async () => {
    if (!runbookId) return
    try {
      setLoading(true)
      const data = await api.getRunbook(runbookId)
      setRunbook(data)
      const initialData: Record<string, string> = {}
      data.sections.forEach((section: SectionResponse) => {
        initialData[section.id] = section.bodyMarkdown
      })
      setSectionData(initialData)
      setError(null)
    } catch (err) {
      setError('Failed to load runbook')
      console.error(err)
    } finally {
      setLoading(false)
    }
  }, [runbookId])

  useEffect(() => {
    loadRunbook()
  }, [loadRunbook])

  const handleSaveSection = async (section: SectionResponse) => {
    if (!runbookId) return
    try {
      setSaving(true)
      const request: SectionRequest = {
        key: section.key,
        title: section.title,
        order: section.order,
        bodyMarkdown: sectionData[section.id] || section.bodyMarkdown,
      }
      await api.updateSection(runbookId, section.id, request)
      setEditingSection(null)
      loadRunbook()
    } catch (err) {
      setError('Failed to save section')
      console.error(err)
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <div className="loading">Loading...</div>
  if (!runbook) return <div className="error">Runbook not found</div>

  return (
    <div>
      <div className="page-header">
        <div>
          <h2>{runbook.title}</h2>
          <p>Status: {runbook.status} | Version: {runbook.version}</p>
          {runbook.summary && <p>{runbook.summary}</p>}
        </div>
      </div>

      {error && <div className="error">{error}</div>}

      <h3>Sections</h3>
      {runbook.sections.map((section) => (
        <div key={section.id} className="section">
          <div className="section-header">
            <h4>{section.title}</h4>
            {editingSection === section.id ? (
              <div>
                <button
                  className="btn btn-secondary"
                  onClick={() => setEditingSection(null)}
                  disabled={saving}
                >
                  Cancel
                </button>{' '}
                <button
                  className="btn btn-primary"
                  onClick={() => handleSaveSection(section)}
                  disabled={saving}
                >
                  {saving ? 'Saving...' : 'Save'}
                </button>
              </div>
            ) : (
              <button
                className="btn btn-primary"
                onClick={() => setEditingSection(section.id)}
              >
                Edit
              </button>
            )}
          </div>
          {editingSection === section.id ? (
            <textarea
              className="form-group"
              style={{ width: '100%', minHeight: '200px', fontFamily: 'monospace' }}
              value={sectionData[section.id] || section.bodyMarkdown}
              onChange={(e) =>
                setSectionData({ ...sectionData, [section.id]: e.target.value })
              }
            />
          ) : (
            <pre style={{ whiteSpace: 'pre-wrap', fontFamily: 'monospace', fontSize: '0.9rem' }}>
              {section.bodyMarkdown}
            </pre>
          )}
        </div>
      ))}
    </div>
  )
}
