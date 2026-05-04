import api from './client'

export interface ProjectDto {
  id: number
  name: string
  description: string | null
  createdAt: string
  ownerId: string
  ownerName: string | null
  ownerEmail: string | null
  taskCount: number
}

export interface ProjectInput {
  name: string
  description?: string | null
  ownerId: string
}

export interface OwnerDto {
  id: string
  fullName: string | null
  email: string | null
}

export const Projects = {
  list: () => api.get<ProjectDto[]>('/api/projects').then((r) => r.data),
  get: (id: number) => api.get<ProjectDto>(`/api/projects/${id}`).then((r) => r.data),
  create: (input: ProjectInput) =>
    api.post<ProjectDto>('/api/projects', input).then((r) => r.data),
  update: (id: number, input: ProjectInput) =>
    api.put(`/api/projects/${id}`, input).then(() => {}),
  remove: (id: number) => api.delete(`/api/projects/${id}`).then(() => {}),
}

export const Owners = {
  list: () => api.get<OwnerDto[]>('/api/owners').then((r) => r.data),
}
