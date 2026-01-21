export interface ServiceResponse {
  id: string
  name: string
  slug: string
  description?: string
  repo?: {
    provider?: string
    owner?: string
    name?: string
    url?: string
    defaultBranch?: string
    primaryLanguage?: string
  }
  createdAt: string
  updatedAt: string
}

export interface ServiceRequest {
  name: string
  slug: string
  description?: string
  repo?: {
    provider?: string
    owner?: string
    name?: string
    url?: string
    defaultBranch?: string
    primaryLanguage?: string
  }
}

export interface RepoAnalysisResult {
  hasDockerfile: boolean
  hasCompose: boolean
  hasKubernetes: boolean
  hasGithubActions: boolean
  detectedPorts: number[]
  appType: string
  hasReadme: boolean
  hasLaunchSettings: boolean
  hasCsproj: boolean
  primaryLanguage?: string
  technologyStack?: string[]
}

export interface RunbookResponse {
  id: string
  serviceId: string
  title: string
  status: string
  version: number
  summary?: string
  origin: string
  analysis?: RepoAnalysisResult
  sections: SectionResponse[]
  variables: VariableResponse[]
  createdAt: string
  updatedAt: string
}

export interface RunbookRequest {
  title: string
  summary?: string
}

export interface SectionResponse {
  id: string
  key: string
  title: string
  order: number
  bodyMarkdown: string
}

export interface SectionRequest {
  key: string
  title: string
  order: number
  bodyMarkdown: string
}

export interface VariableResponse {
  id: string
  name: string
  valueHint?: string
  isSecret: boolean
  description?: string
}

export interface VariableRequest {
  name: string
  valueHint?: string
  isSecret: boolean
  description?: string
}

export interface UserResponse {
  id: string
  email: string
  displayName?: string
  createdAt: string
  updatedAt: string
}

// Incident types
export interface IncidentResponse {
  id: string
  serviceId: string
  runbookId?: string
  runbookTitle?: string
  title: string
  severity: string
  status: string
  startedAt: string
  endedAt?: string
  summaryMarkdown?: string
  createdAt: string
  updatedAt: string
}

export interface IncidentRequest {
  title: string
  severity: string
  startedAt: string
  summaryMarkdown?: string
}

export interface IncidentUpdateRequest {
  title?: string
  severity?: string
  status?: string
  endedAt?: string
  summaryMarkdown?: string
}

// Timeline types
export interface TimelineEntryResponse {
  id: string
  incidentId: string
  entryType: string
  occurredAt: string
  bodyMarkdown: string
  createdAt: string
}

export interface TimelineEntryRequest {
  entryType: string
  bodyMarkdown: string
}

// Constants
export const IncidentStatus = {
  Open: 'open',
  Investigating: 'investigating',
  Mitigated: 'mitigated',
  Resolved: 'resolved',
} as const

export const IncidentSeverity = {
  Sev1: 'sev1',
  Sev2: 'sev2',
  Sev3: 'sev3',
  Sev4: 'sev4',
} as const

export const TimelineEntryType = {
  Note: 'note',
  Action: 'action',
  Decision: 'decision',
  Observation: 'observation',
} as const
