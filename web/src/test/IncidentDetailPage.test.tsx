import { describe, it, expect, beforeEach, vi } from 'vitest'
import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import IncidentDetailPage from '../pages/IncidentDetailPage'
import * as api from '../services/api'
import type { IncidentResponse, TimelineEntryResponse, RunbookResponse } from '../types'
import { IncidentSeverity, IncidentStatus, TimelineEntryType } from '../types'

const mockIncident: IncidentResponse = {
  id: 'incident-1',
  serviceId: 'service-1',
  runbookId: 'runbook-1',
  runbookTitle: 'Test Runbook',
  title: 'API Outage',
  severity: IncidentSeverity.Sev2,
  status: IncidentStatus.Investigating,
  startedAt: '2024-01-15T10:30:00Z',
  createdAt: '2024-01-15T10:30:00Z',
  updatedAt: '2024-01-15T10:30:00Z',
}

const mockIncidentNoRunbook: IncidentResponse = {
  ...mockIncident,
  id: 'incident-2',
  runbookId: undefined,
  runbookTitle: undefined,
}

const mockRunbook: RunbookResponse = {
  id: 'runbook-1',
  serviceId: 'service-1',
  title: 'Test Runbook',
  status: 'published',
  version: 1,
  origin: 'manual',
  sections: [
    { id: 'section-1', key: 'health-checks', title: 'Health Checks', order: 1, bodyMarkdown: '## Health' },
    { id: 'section-2', key: 'rollback', title: 'Rollback Steps', order: 2, bodyMarkdown: '## Rollback' },
  ],
  variables: [],
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
}

const mockTimelineEntry: TimelineEntryResponse = {
  id: 'entry-1',
  incidentId: 'incident-1',
  entryType: TimelineEntryType.Note,
  occurredAt: '2024-01-15T10:35:00Z',
  bodyMarkdown: 'Started investigating the issue',
  createdAt: '2024-01-15T10:35:00Z',
}

const mockTimelineEntries: TimelineEntryResponse[] = [
  mockTimelineEntry,
  {
    id: 'entry-2',
    incidentId: 'incident-1',
    entryType: TimelineEntryType.Action,
    occurredAt: '2024-01-15T10:40:00Z',
    bodyMarkdown: 'Restarted the service',
    createdAt: '2024-01-15T10:40:00Z',
  },
]

const renderWithRouter = (incidentId: string) => {
  return render(
    <MemoryRouter initialEntries={[`/incidents/${incidentId}`]}>
      <Routes>
        <Route path="/incidents/:incidentId" element={<IncidentDetailPage />} />
        <Route path="/services/:serviceId" element={<div>Service Page</div>} />
        <Route path="/runbooks/:runbookId" element={<div>Runbook Page</div>} />
      </Routes>
    </MemoryRouter>
  )
}

describe('IncidentDetailPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('renders incident header with title, severity and status', async () => {
    vi.spyOn(api.api, 'getIncident').mockResolvedValue(mockIncident)
    vi.spyOn(api.api, 'getIncidentTimeline').mockResolvedValue([])
    vi.spyOn(api.api, 'getRunbook').mockResolvedValue(mockRunbook)

    renderWithRouter('incident-1')

    await waitFor(() => {
      expect(screen.getByText('API Outage')).toBeInTheDocument()
      expect(screen.getByText('SEV2')).toBeInTheDocument()
      expect(screen.getByText('investigating')).toBeInTheDocument()
    })
  })

  it('displays timeline entries in order', async () => {
    vi.spyOn(api.api, 'getIncident').mockResolvedValue(mockIncident)
    vi.spyOn(api.api, 'getIncidentTimeline').mockResolvedValue(mockTimelineEntries)
    vi.spyOn(api.api, 'getRunbook').mockResolvedValue(mockRunbook)

    renderWithRouter('incident-1')

    await waitFor(() => {
      expect(screen.getByText('Started investigating the issue')).toBeInTheDocument()
      expect(screen.getByText('Restarted the service')).toBeInTheDocument()
    })
  })

  it('shows timeline entry form', async () => {
    vi.spyOn(api.api, 'getIncident').mockResolvedValue(mockIncident)
    vi.spyOn(api.api, 'getIncidentTimeline').mockResolvedValue([])
    vi.spyOn(api.api, 'getRunbook').mockResolvedValue(mockRunbook)

    renderWithRouter('incident-1')

    await waitFor(() => {
      expect(screen.getByText('Add Timeline Entry')).toBeInTheDocument()
      expect(screen.getByPlaceholderText('What happened?')).toBeInTheDocument()
    })
  })

  it('adds timeline entry when form is submitted', async () => {
    const addTimelineEntry = vi.spyOn(api.api, 'addTimelineEntry').mockResolvedValue(mockTimelineEntry)
    vi.spyOn(api.api, 'getIncident').mockResolvedValue(mockIncident)
    vi.spyOn(api.api, 'getIncidentTimeline').mockResolvedValue([])
    vi.spyOn(api.api, 'getRunbook').mockResolvedValue(mockRunbook)

    renderWithRouter('incident-1')

    await waitFor(() => {
      expect(screen.getByPlaceholderText('What happened?')).toBeInTheDocument()
    })

    fireEvent.change(screen.getByPlaceholderText('What happened?'), {
      target: { value: 'Identified root cause' },
    })

    fireEvent.click(screen.getByText('Add'))

    await waitFor(() => {
      expect(addTimelineEntry).toHaveBeenCalledWith('incident-1', {
        entryType: TimelineEntryType.Note,
        bodyMarkdown: 'Identified root cause',
      })
    })
  })

  it('displays quick links to runbook sections when runbook is attached', async () => {
    vi.spyOn(api.api, 'getIncident').mockResolvedValue(mockIncident)
    vi.spyOn(api.api, 'getIncidentTimeline').mockResolvedValue([])
    vi.spyOn(api.api, 'getRunbook').mockResolvedValue(mockRunbook)

    renderWithRouter('incident-1')

    await waitFor(() => {
      expect(screen.getByText('Runbook: Test Runbook')).toBeInTheDocument()
      expect(screen.getByText('Open Full Runbook')).toBeInTheDocument()
      expect(screen.getByText('Health Checks')).toBeInTheDocument()
      expect(screen.getByText('Rollback Steps')).toBeInTheDocument()
    })
  })

  it('does not display quick links when no runbook attached', async () => {
    vi.spyOn(api.api, 'getIncident').mockResolvedValue(mockIncidentNoRunbook)
    vi.spyOn(api.api, 'getIncidentTimeline').mockResolvedValue([])

    renderWithRouter('incident-2')

    await waitFor(() => {
      expect(screen.getByText('API Outage')).toBeInTheDocument()
    })

    expect(screen.queryByText('Runbook:')).not.toBeInTheDocument()
    expect(screen.queryByText('Open Full Runbook')).not.toBeInTheDocument()
  })

  it('shows status change buttons', async () => {
    vi.spyOn(api.api, 'getIncident').mockResolvedValue(mockIncident)
    vi.spyOn(api.api, 'getIncidentTimeline').mockResolvedValue([])
    vi.spyOn(api.api, 'getRunbook').mockResolvedValue(mockRunbook)

    renderWithRouter('incident-1')

    await waitFor(() => {
      // From investigating, can go to mitigated or resolved
      expect(screen.getByText('Mark mitigated')).toBeInTheDocument()
      expect(screen.getByText('Mark resolved')).toBeInTheDocument()
    })
  })

  it('updates status when status button is clicked', async () => {
    const transitionStatus = vi.spyOn(api.api, 'transitionIncidentStatus').mockResolvedValue({
      id: 'incident-1',
      previousStatus: IncidentStatus.Investigating,
      newStatus: IncidentStatus.Mitigated,
      updatedAt: '2024-01-15T10:45:00Z',
    })
    vi.spyOn(api.api, 'getIncident').mockResolvedValue(mockIncident)
    vi.spyOn(api.api, 'getIncidentTimeline').mockResolvedValue([])
    vi.spyOn(api.api, 'getRunbook').mockResolvedValue(mockRunbook)

    renderWithRouter('incident-1')

    await waitFor(() => {
      expect(screen.getByText('Mark mitigated')).toBeInTheDocument()
    })

    fireEvent.click(screen.getByText('Mark mitigated'))

    await waitFor(() => {
      expect(transitionStatus).toHaveBeenCalledWith('incident-1', IncidentStatus.Mitigated)
    })
  })

  it('shows no entries message when timeline is empty', async () => {
    vi.spyOn(api.api, 'getIncident').mockResolvedValue(mockIncident)
    vi.spyOn(api.api, 'getIncidentTimeline').mockResolvedValue([])
    vi.spyOn(api.api, 'getRunbook').mockResolvedValue(mockRunbook)

    renderWithRouter('incident-1')

    await waitFor(() => {
      expect(screen.getByText('No timeline entries yet. Add one above to start documenting.')).toBeInTheDocument()
    })
  })

  it('displays back link to service page', async () => {
    vi.spyOn(api.api, 'getIncident').mockResolvedValue(mockIncident)
    vi.spyOn(api.api, 'getIncidentTimeline').mockResolvedValue([])
    vi.spyOn(api.api, 'getRunbook').mockResolvedValue(mockRunbook)

    renderWithRouter('incident-1')

    await waitFor(() => {
      expect(screen.getByText('Back to Service')).toBeInTheDocument()
    })
  })

  it('shows different entry type icons in timeline', async () => {
    const mixedEntries: TimelineEntryResponse[] = [
      { ...mockTimelineEntry, id: '1', entryType: TimelineEntryType.Note },
      { ...mockTimelineEntry, id: '2', entryType: TimelineEntryType.Action, bodyMarkdown: 'Took action' },
      { ...mockTimelineEntry, id: '3', entryType: TimelineEntryType.Decision, bodyMarkdown: 'Made decision' },
      { ...mockTimelineEntry, id: '4', entryType: TimelineEntryType.Observation, bodyMarkdown: 'Observed something' },
    ]

    vi.spyOn(api.api, 'getIncident').mockResolvedValue(mockIncident)
    vi.spyOn(api.api, 'getIncidentTimeline').mockResolvedValue(mixedEntries)
    vi.spyOn(api.api, 'getRunbook').mockResolvedValue(mockRunbook)

    renderWithRouter('incident-1')

    await waitFor(() => {
      expect(screen.getByText('N')).toBeInTheDocument() // Note icon
      expect(screen.getByText('A')).toBeInTheDocument() // Action icon
      expect(screen.getByText('D')).toBeInTheDocument() // Decision icon
      expect(screen.getByText('O')).toBeInTheDocument() // Observation icon
    })
  })
})
