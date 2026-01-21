const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'
const USER_EMAIL = import.meta.env.VITE_USER_EMAIL || 'josh@local'

async function fetchApi<T>(endpoint: string, options?: RequestInit): Promise<T> {
  const url = `${API_BASE_URL}${endpoint}`

  const headers = {
    'Content-Type': 'application/json',
    'X-User-Email': USER_EMAIL,
    ...options?.headers,
  }

  const response = await fetch(url, {
    ...options,
    headers,
    credentials: 'include', // Include cookies for authentication
  })

  if (!response.ok) {
    throw new Error(`API error: ${response.status} ${response.statusText}`)
  }

  if (response.status === 204) {
    return null as T
  }

  return response.json()
}

export const api = {
  // Health
  health: () => fetchApi<{ status: string }>('/api/health'),

  // Auth
  getCurrentUser: () => fetchApi<any>('/auth/me'),
  logout: () => fetchApi<void>('/auth/logout', { method: 'POST' }),

  // Services
  getServices: () => fetchApi<Array<any>>('/api/services'),
  getService: (id: string) => fetchApi<any>(`/api/services/${id}`),
  createService: (data: any) => fetchApi<any>('/api/services', {
    method: 'POST',
    body: JSON.stringify(data),
  }),
  updateService: (id: string, data: any) => fetchApi<any>(`/api/services/${id}`, {
    method: 'PATCH',
    body: JSON.stringify(data),
  }),
  deleteService: (id: string) => fetchApi<void>(`/api/services/${id}`, {
    method: 'DELETE',
  }),

  // Runbooks
  getServiceRunbooks: (serviceId: string) => fetchApi<Array<any>>(`/api/services/${serviceId}/runbooks`),
  createRunbook: (serviceId: string, data: any) => fetchApi<any>(`/api/services/${serviceId}/runbooks`, {
    method: 'POST',
    body: JSON.stringify(data),
  }),
  generateRunbook: (serviceId: string) => fetchApi<any>(`/api/services/${serviceId}/runbooks/generate`, {
    method: 'POST',
  }),
  getRunbook: (id: string) => fetchApi<any>(`/api/runbooks/${id}`),
  updateRunbook: (id: string, data: any) => fetchApi<any>(`/api/runbooks/${id}`, {
    method: 'PATCH',
    body: JSON.stringify(data),
  }),
  deleteRunbook: (id: string) => fetchApi<void>(`/api/runbooks/${id}`, {
    method: 'DELETE',
  }),

  // Sections
  updateSection: (runbookId: string, sectionId: string, data: any) => fetchApi<any>(
    `/api/runbooks/${runbookId}/sections/${sectionId}`,
    {
      method: 'PATCH',
      body: JSON.stringify(data),
    }
  ),

  // Variables
  createVariable: (runbookId: string, data: any) => fetchApi<any>(`/api/runbooks/${runbookId}/variables`, {
    method: 'POST',
    body: JSON.stringify(data),
  }),
  updateVariable: (runbookId: string, variableId: string, data: any) => fetchApi<any>(
    `/api/runbooks/${runbookId}/variables/${variableId}`,
    {
      method: 'PATCH',
      body: JSON.stringify(data),
    }
  ),
  deleteVariable: (runbookId: string, variableId: string) => fetchApi<void>(
    `/api/runbooks/${runbookId}/variables/${variableId}`,
    {
      method: 'DELETE',
    }
  ),

  // GitHub
  getGitHubRepos: (page = 1, perPage = 30) => fetchApi<Array<any>>(`/api/github/repos?page=${page}&perPage=${perPage}`),
  linkRepoToService: (serviceId: string, data: any) => fetchApi<any>(`/api/services/${serviceId}/link-repo`, {
    method: 'PATCH',
    body: JSON.stringify(data),
  }),

  // Incidents
  getServiceIncidents: (serviceId: string) => fetchApi<Array<any>>(`/api/services/${serviceId}/incidents`),
  createIncident: (serviceId: string, data: any) => fetchApi<any>(`/api/services/${serviceId}/incidents`, {
    method: 'POST',
    body: JSON.stringify(data),
  }),
  getIncident: (id: string) => fetchApi<any>(`/api/incidents/${id}`),
  updateIncident: (id: string, data: any) => fetchApi<any>(`/api/incidents/${id}`, {
    method: 'PATCH',
    body: JSON.stringify(data),
  }),
  deleteIncident: (id: string) => fetchApi<void>(`/api/incidents/${id}`, {
    method: 'DELETE',
  }),

  // Timeline
  getIncidentTimeline: (incidentId: string) => fetchApi<Array<any>>(`/api/incidents/${incidentId}/timeline`),
  addTimelineEntry: (incidentId: string, data: any) => fetchApi<any>(`/api/incidents/${incidentId}/timeline`, {
    method: 'POST',
    body: JSON.stringify(data),
  }),
}
