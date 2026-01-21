import { describe, it, expect, beforeEach, vi } from 'vitest'
import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import ServiceDetailPage from '../pages/ServiceDetailPage'
import * as api from '../services/api'
import type { ServiceResponse, RunbookResponse, IncidentResponse } from '../types'
import { IncidentSeverity, IncidentStatus } from '../types'

const mockService: ServiceResponse = {
  id: 'service-1',
  name: 'Test Service',
  slug: 'test-service',
  description: 'A test service',
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
}

const mockServiceWithRepo: ServiceResponse = {
  ...mockService,
  repo: {
    provider: 'github',
    owner: 'testuser',
    name: 'testrepo',
    url: 'https://github.com/testuser/testrepo',
    defaultBranch: 'main',
    primaryLanguage: 'TypeScript',
  },
}

const mockRunbook: RunbookResponse = {
  id: 'runbook-1',
  serviceId: 'service-1',
  title: 'Test Runbook',
  status: 'draft',
  version: 1,
  summary: 'A test runbook',
  origin: 'manual',
  sections: [],
  variables: [],
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
}

const mockGeneratedRunbook: RunbookResponse = {
  ...mockRunbook,
  id: 'runbook-2',
  title: 'Generated Runbook',
  origin: 'generated',
  analysis: {
    hasDockerfile: true,
    hasCompose: false,
    hasKubernetes: false,
    hasGithubActions: true,
    detectedPorts: [3000],
    appType: 'node',
    hasReadme: true,
    hasLaunchSettings: false,
    hasCsproj: false,
    primaryLanguage: 'TypeScript',
    technologyStack: ['React', 'TypeScript'],
  },
}

const mockIncident: IncidentResponse = {
  id: 'incident-1',
  serviceId: 'service-1',
  title: 'API Outage',
  severity: IncidentSeverity.Sev2,
  status: IncidentStatus.Investigating,
  startedAt: '2024-01-15T10:30:00Z',
  createdAt: '2024-01-15T10:30:00Z',
  updatedAt: '2024-01-15T10:30:00Z',
}

const renderWithRouter = (serviceId: string) => {
  return render(
    <MemoryRouter initialEntries={[`/services/${serviceId}`]}>
      <Routes>
        <Route path="/services/:serviceId" element={<ServiceDetailPage />} />
      </Routes>
    </MemoryRouter>
  )
}

describe('ServiceDetailPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('renders Generate Draft button disabled when no repo is linked', async () => {
    vi.spyOn(api.api, 'getService').mockResolvedValue(mockService)
    vi.spyOn(api.api, 'getServiceRunbooks').mockResolvedValue([])
    vi.spyOn(api.api, 'getServiceIncidents').mockResolvedValue([])

    renderWithRouter('service-1')

    await waitFor(() => {
      const generateButton = screen.getByText('Generate Draft')
      expect(generateButton).toBeInTheDocument()
      expect(generateButton).toBeDisabled()
    })
  })

  it('renders Generate Draft button enabled when repo is linked', async () => {
    vi.spyOn(api.api, 'getService').mockResolvedValue(mockServiceWithRepo)
    vi.spyOn(api.api, 'getServiceRunbooks').mockResolvedValue([])
    vi.spyOn(api.api, 'getServiceIncidents').mockResolvedValue([])

    renderWithRouter('service-1')

    await waitFor(() => {
      const generateButton = screen.getByText('Generate Draft')
      expect(generateButton).toBeInTheDocument()
      expect(generateButton).not.toBeDisabled()
    })
  })

  it('shows confirmation modal when runbooks exist and Generate Draft is clicked', async () => {
    vi.spyOn(api.api, 'getService').mockResolvedValue(mockServiceWithRepo)
    vi.spyOn(api.api, 'getServiceRunbooks').mockResolvedValue([mockRunbook])
    vi.spyOn(api.api, 'getServiceIncidents').mockResolvedValue([])

    renderWithRouter('service-1')

    await waitFor(() => {
      expect(screen.getByText('Generate Draft')).toBeInTheDocument()
    })

    fireEvent.click(screen.getByText('Generate Draft'))

    await waitFor(() => {
      expect(screen.getByText('Generate New Runbook')).toBeInTheDocument()
      expect(screen.getByText(/already has runbooks/)).toBeInTheDocument()
    })
  })

  it('does not show confirmation modal when no runbooks exist', async () => {
    const generateRunbook = vi.spyOn(api.api, 'generateRunbook').mockResolvedValue(mockGeneratedRunbook)
    vi.spyOn(api.api, 'getService').mockResolvedValue(mockServiceWithRepo)
    vi.spyOn(api.api, 'getServiceRunbooks').mockResolvedValue([])
    vi.spyOn(api.api, 'getServiceIncidents').mockResolvedValue([])

    renderWithRouter('service-1')

    await waitFor(() => {
      expect(screen.getByText('Generate Draft')).toBeInTheDocument()
    })

    fireEvent.click(screen.getByText('Generate Draft'))

    // Should call generateRunbook directly without showing modal
    await waitFor(() => {
      expect(generateRunbook).toHaveBeenCalledWith('service-1')
    })
  })

  it('displays origin badge for manual runbooks', async () => {
    vi.spyOn(api.api, 'getService').mockResolvedValue(mockServiceWithRepo)
    vi.spyOn(api.api, 'getServiceRunbooks').mockResolvedValue([mockRunbook])
    vi.spyOn(api.api, 'getServiceIncidents').mockResolvedValue([])

    renderWithRouter('service-1')

    await waitFor(() => {
      expect(screen.getByText('Manual')).toBeInTheDocument()
    })
  })

  it('displays origin badge for generated runbooks', async () => {
    vi.spyOn(api.api, 'getService').mockResolvedValue(mockServiceWithRepo)
    vi.spyOn(api.api, 'getServiceRunbooks').mockResolvedValue([mockGeneratedRunbook])
    vi.spyOn(api.api, 'getServiceIncidents').mockResolvedValue([])

    renderWithRouter('service-1')

    await waitFor(() => {
      expect(screen.getByText('Generated')).toBeInTheDocument()
    })
  })

  it('shows loading state during generation', async () => {
    // Make generateRunbook hang for a bit
    vi.spyOn(api.api, 'generateRunbook').mockImplementation(
      () => new Promise((resolve) => setTimeout(() => resolve(mockGeneratedRunbook), 1000))
    )
    vi.spyOn(api.api, 'getService').mockResolvedValue(mockServiceWithRepo)
    vi.spyOn(api.api, 'getServiceRunbooks').mockResolvedValue([])
    vi.spyOn(api.api, 'getServiceIncidents').mockResolvedValue([])

    renderWithRouter('service-1')

    await waitFor(() => {
      expect(screen.getByText('Generate Draft')).toBeInTheDocument()
    })

    fireEvent.click(screen.getByText('Generate Draft'))

    // Button should show loading state
    await waitFor(() => {
      expect(screen.getByText('Generating...')).toBeInTheDocument()
    })
  })

  it('shows error message when generation fails', async () => {
    vi.spyOn(api.api, 'generateRunbook').mockRejectedValue(new Error('Generation failed'))
    vi.spyOn(api.api, 'getService').mockResolvedValue(mockServiceWithRepo)
    vi.spyOn(api.api, 'getServiceRunbooks').mockResolvedValue([])
    vi.spyOn(api.api, 'getServiceIncidents').mockResolvedValue([])

    renderWithRouter('service-1')

    await waitFor(() => {
      expect(screen.getByText('Generate Draft')).toBeInTheDocument()
    })

    fireEvent.click(screen.getByText('Generate Draft'))

    await waitFor(() => {
      expect(screen.getByText('Generation failed')).toBeInTheDocument()
    })
  })

  // Incident tests
  it('displays incidents list when incidents exist', async () => {
    vi.spyOn(api.api, 'getService').mockResolvedValue(mockService)
    vi.spyOn(api.api, 'getServiceRunbooks').mockResolvedValue([])
    vi.spyOn(api.api, 'getServiceIncidents').mockResolvedValue([mockIncident])

    renderWithRouter('service-1')

    await waitFor(() => {
      expect(screen.getByText('API Outage')).toBeInTheDocument()
      expect(screen.getByText('SEV2')).toBeInTheDocument()
      expect(screen.getByText('investigating')).toBeInTheDocument()
    })
  })

  it('shows New Incident button', async () => {
    vi.spyOn(api.api, 'getService').mockResolvedValue(mockService)
    vi.spyOn(api.api, 'getServiceRunbooks').mockResolvedValue([])
    vi.spyOn(api.api, 'getServiceIncidents').mockResolvedValue([])

    renderWithRouter('service-1')

    await waitFor(() => {
      expect(screen.getByText('+ New Incident')).toBeInTheDocument()
    })
  })

  it('shows incident form when New Incident is clicked', async () => {
    vi.spyOn(api.api, 'getService').mockResolvedValue(mockService)
    vi.spyOn(api.api, 'getServiceRunbooks').mockResolvedValue([])
    vi.spyOn(api.api, 'getServiceIncidents').mockResolvedValue([])

    renderWithRouter('service-1')

    await waitFor(() => {
      expect(screen.getByText('+ New Incident')).toBeInTheDocument()
    })

    fireEvent.click(screen.getByText('+ New Incident'))

    await waitFor(() => {
      expect(screen.getByPlaceholderText('Brief description of the incident')).toBeInTheDocument()
      expect(screen.getByText('SEV1 - Critical')).toBeInTheDocument()
    })
  })

  it('creates incident when form is submitted', async () => {
    const createIncident = vi.spyOn(api.api, 'createIncident').mockResolvedValue(mockIncident)
    vi.spyOn(api.api, 'getService').mockResolvedValue(mockService)
    vi.spyOn(api.api, 'getServiceRunbooks').mockResolvedValue([])
    vi.spyOn(api.api, 'getServiceIncidents').mockResolvedValue([])

    renderWithRouter('service-1')

    await waitFor(() => {
      expect(screen.getByText('+ New Incident')).toBeInTheDocument()
    })

    fireEvent.click(screen.getByText('+ New Incident'))

    await waitFor(() => {
      expect(screen.getByPlaceholderText('Brief description of the incident')).toBeInTheDocument()
    })

    fireEvent.change(screen.getByPlaceholderText('Brief description of the incident'), {
      target: { value: 'Database connection failure' },
    })

    fireEvent.click(screen.getByText('Create Incident'))

    await waitFor(() => {
      expect(createIncident).toHaveBeenCalled()
    })
  })

  it('shows no incidents message when empty', async () => {
    vi.spyOn(api.api, 'getService').mockResolvedValue(mockService)
    vi.spyOn(api.api, 'getServiceRunbooks').mockResolvedValue([])
    vi.spyOn(api.api, 'getServiceIncidents').mockResolvedValue([])

    renderWithRouter('service-1')

    await waitFor(() => {
      expect(screen.getByText('No incidents recorded.')).toBeInTheDocument()
    })
  })
})
