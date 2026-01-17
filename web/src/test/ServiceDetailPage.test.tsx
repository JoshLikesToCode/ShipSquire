import { describe, it, expect, beforeEach, vi } from 'vitest'
import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import ServiceDetailPage from '../pages/ServiceDetailPage'
import * as api from '../services/api'
import type { ServiceResponse, RunbookResponse } from '../types'

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

    renderWithRouter('service-1')

    await waitFor(() => {
      expect(screen.getByText('Manual')).toBeInTheDocument()
    })
  })

  it('displays origin badge for generated runbooks', async () => {
    vi.spyOn(api.api, 'getService').mockResolvedValue(mockServiceWithRepo)
    vi.spyOn(api.api, 'getServiceRunbooks').mockResolvedValue([mockGeneratedRunbook])

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

    renderWithRouter('service-1')

    await waitFor(() => {
      expect(screen.getByText('Generate Draft')).toBeInTheDocument()
    })

    fireEvent.click(screen.getByText('Generate Draft'))

    await waitFor(() => {
      expect(screen.getByText('Generation failed')).toBeInTheDocument()
    })
  })
})
