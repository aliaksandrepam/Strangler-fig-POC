import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { Projects } from '../api/projects'
import Calendar from '../components/Calendar'

/**
 * Mirrors PocApp/Views/Projects/Details.cshtml — same heading row,
 * same definition list, same calendar (filtered to this project).
 */
export default function ProjectDetail() {
  const { id } = useParams()
  const projectId = Number(id)
  const { data, isLoading, error } = useQuery({
    queryKey: ['project', projectId],
    queryFn: () => Projects.get(projectId),
    enabled: !Number.isNaN(projectId),
  })

  if (isLoading) return <p className="text-muted">Loading…</p>
  if (error) return <p className="text-danger">Failed: {(error as Error).message}</p>
  if (!data) return <p className="text-danger">Not found.</p>

  return (
    <>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <h2 className="mb-0">{data.name}</h2>
        <div>
          <Link className="btn btn-outline-secondary" to={`/${data.id}/edit`}>Edit</Link>{' '}
          <Link className="btn btn-outline-secondary" to="/">Back</Link>
        </div>
      </div>

      <div className="row">
        <div className="col-lg-7">
          <dl className="row">
            <dt className="col-sm-3">Owner</dt>
            <dd className="col-sm-9">{data.ownerName || data.ownerEmail}</dd>
            <dt className="col-sm-3">Created</dt>
            <dd className="col-sm-9">{new Date(data.createdAt).toLocaleString()}</dd>
            <dt className="col-sm-3">Description</dt>
            <dd className="col-sm-9">{data.description || '—'}</dd>
            <dt className="col-sm-3">Tasks</dt>
            <dd className="col-sm-9">{data.taskCount}</dd>
          </dl>

          <p className="small text-muted">
            Task management for this project still lives in the legacy MVC app —{' '}
            <a href={`/Projects/Details/${data.id}`}>open full view</a>.
          </p>
        </div>
        <div className="col-lg-5">
          <Calendar projectId={data.id} />
        </div>
      </div>
    </>
  )
}
