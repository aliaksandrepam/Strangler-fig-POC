import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { Projects } from '../api/projects'
import Calendar from '../components/Calendar'

/**
 * Mirrors PocApp/Views/Projects/Index.cshtml — same heading, same table
 * columns, same calendar on the right. Pixel-equivalent to the MVC page.
 */
export default function ProjectsList() {
  const qc = useQueryClient()
  const { data, isLoading, error } = useQuery({
    queryKey: ['projects'],
    queryFn: Projects.list,
  })

  const del = useMutation({
    mutationFn: (id: number) => Projects.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['projects'] }),
  })

  if (isLoading) return <p className="text-muted">Loading projects…</p>
  if (error) return <p className="text-danger">Failed to load: {(error as Error).message}</p>

  return (
    <>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <h2 className="mb-0">Projects</h2>
        <Link to="/new" className="btn btn-primary">+ New project</Link>
      </div>

      <div className="row">
        <div className="col-lg-7">
          <table className="table table-striped table-hover">
            <thead>
              <tr>
                <th>Name</th>
                <th>Owner</th>
                <th className="text-end">Tasks</th>
                <th>Created</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {data?.map((p) => (
                <tr key={p.id}>
                  <td>
                    <Link to={`/${p.id}`}>{p.name}</Link>
                    {p.description && (
                      <div className="small text-muted">{p.description}</div>
                    )}
                  </td>
                  <td>{p.ownerName || p.ownerEmail || '-'}</td>
                  <td className="text-end">{p.taskCount}</td>
                  <td>{new Date(p.createdAt).toLocaleDateString('en-CA')}</td>
                  <td className="text-end">
                    <Link className="btn btn-sm btn-outline-secondary" to={`/${p.id}/edit`}>
                      Edit
                    </Link>{' '}
                    <button
                      className="btn btn-sm btn-outline-danger"
                      onClick={() => {
                        if (confirm(`Delete "${p.name}"? This also deletes its tasks.`)) {
                          del.mutate(p.id)
                        }
                      }}
                      disabled={del.isPending}
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div className="col-lg-5">
          <Calendar />
        </div>
      </div>
    </>
  )
}
