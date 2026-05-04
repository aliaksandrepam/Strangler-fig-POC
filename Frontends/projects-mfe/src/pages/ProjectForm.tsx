import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { Owners, Projects, type ProjectInput } from '../api/projects'
import { AxiosError } from 'axios'

interface Props {
  mode: 'create' | 'edit'
}

/**
 * Mirrors PocApp/Views/Projects/Create.cshtml + Edit.cshtml — same
 * Bootstrap form classes, same labels, same column width.
 */
export default function ProjectForm({ mode }: Props) {
  const navigate = useNavigate()
  const qc = useQueryClient()
  const { id } = useParams()
  const projectId = mode === 'edit' ? Number(id) : undefined

  const owners = useQuery({ queryKey: ['owners'], queryFn: Owners.list })
  const existing = useQuery({
    queryKey: ['project', projectId],
    queryFn: () => Projects.get(projectId!),
    enabled: mode === 'edit' && !!projectId,
  })

  const [form, setForm] = useState<ProjectInput>({ name: '', description: '', ownerId: '' })
  const [errors, setErrors] = useState<Record<string, string[]>>({})

  useEffect(() => {
    if (mode === 'edit' && existing.data) {
      setForm({
        name: existing.data.name,
        description: existing.data.description ?? '',
        ownerId: existing.data.ownerId,
      })
    }
  }, [mode, existing.data])

  const save = useMutation({
    mutationFn: () =>
      mode === 'create'
        ? Projects.create(form)
        : Projects.update(projectId!, form).then(() => null),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['projects'] })
      if (projectId) qc.invalidateQueries({ queryKey: ['project', projectId] })
      navigate('/')
    },
    onError: (err: AxiosError<{ errors?: Record<string, string[]> }>) => {
      setErrors(err.response?.data?.errors ?? { _: ['Save failed.'] })
    },
  })

  if (mode === 'edit' && existing.isLoading) return <p className="text-muted">Loading…</p>

  const heading = mode === 'create' ? 'New project' : 'Edit project'

  return (
    <>
      <h2>{heading}</h2>

      <form
        className="col-md-6"
        onSubmit={(e) => {
          e.preventDefault()
          setErrors({})
          save.mutate()
        }}
      >
        {errors._ && <div className="text-danger mb-2">{errors._.join(' ')}</div>}

        <div className="mb-3">
          <label className="form-label">Name</label>
          <input
            className="form-control"
            value={form.name}
            onChange={(e) => setForm({ ...form, name: e.target.value })}
            required
            maxLength={150}
          />
          {errors.Name && <span className="text-danger">{errors.Name.join(' ')}</span>}
        </div>

        <div className="mb-3">
          <label className="form-label">Description</label>
          <textarea
            className="form-control"
            rows={3}
            maxLength={1000}
            value={form.description ?? ''}
            onChange={(e) => setForm({ ...form, description: e.target.value })}
          />
        </div>

        <div className="mb-3">
          <label className="form-label">Owner</label>
          <select
            className="form-select"
            value={form.ownerId}
            onChange={(e) => setForm({ ...form, ownerId: e.target.value })}
            required
          >
            <option value="">-- choose owner --</option>
            {owners.data?.map((o) => (
              <option key={o.id} value={o.id}>
                {o.fullName || o.email || o.id}
              </option>
            ))}
          </select>
          {errors.OwnerId && <span className="text-danger">{errors.OwnerId.join(' ')}</span>}
        </div>

        <button type="submit" className="btn btn-primary" disabled={save.isPending}>
          {save.isPending ? 'Saving…' : mode === 'create' ? 'Create' : 'Save'}
        </button>{' '}
        <Link className="btn btn-link" to="/">Cancel</Link>
      </form>
    </>
  )
}
