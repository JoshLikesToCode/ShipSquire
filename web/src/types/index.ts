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

export interface RunbookResponse {
  id: string
  serviceId: string
  title: string
  status: string
  version: number
  summary?: string
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
