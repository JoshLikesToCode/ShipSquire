import { describe, it, expect, beforeEach, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import App from '../App'
import * as api from '../services/api'

describe('App', () => {
  beforeEach(() => {
    // Reset mocks before each test
    vi.restoreAllMocks()
  })

  it('renders login page when not authenticated', async () => {
    // Mock getCurrentUser to throw an error (not authenticated)
    vi.spyOn(api.api, 'getCurrentUser').mockRejectedValue(new Error('Not authenticated'))

    render(
      <MemoryRouter initialEntries={['/']}>
        <App />
      </MemoryRouter>
    )

    // Wait for the loading state to finish and login page to render
    await waitFor(() => {
      expect(screen.getByText('ShipSquire')).toBeInTheDocument()
      expect(screen.getByText('Login with GitHub')).toBeInTheDocument()
    })
  })

  it('renders app navigation when authenticated', async () => {
    // Mock getCurrentUser to return a user
    vi.spyOn(api.api, 'getCurrentUser').mockResolvedValue({
      id: '1',
      email: 'test@example.com',
      displayName: 'Test User',
      authProvider: 'local',
      isAuthenticated: true,
    })

    // Mock getServices to return empty array
    vi.spyOn(api.api, 'getServices').mockResolvedValue([])

    render(
      <MemoryRouter initialEntries={['/services']}>
        <App />
      </MemoryRouter>
    )

    // Wait for authentication check and navigation to render
    await waitFor(() => {
      // Check for navigation elements
      const headings = screen.getAllByText('ShipSquire')
      expect(headings.length).toBeGreaterThan(0)
      expect(screen.getByText('Services')).toBeInTheDocument()
    })
  })
})
