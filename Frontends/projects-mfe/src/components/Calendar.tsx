import { useQuery } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import api from '../api/client'

interface CalendarTask {
  id: number
  title: string
  dueDate: string // ISO date
  status: 'Todo' | 'InProgress' | 'Done' | 'Blocked'
  projectId: number
  projectName: string | null
}

interface CalendarFeed {
  year: number
  month: number
  tasks: CalendarTask[]
}

interface Props {
  projectId?: number // optional filter
}

const DAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']
const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
]

/**
 * Mirrors PocApp/Views/Shared/Components/Calendar/Default.cshtml — same DOM
 * structure, same class names, same Monday-first grid, same task-pill markup
 * with status colors. Visually identical to the legacy calendar.
 */
export default function Calendar({ projectId }: Props) {
  const today = useMemo(() => {
    const t = new Date()
    return new Date(t.getFullYear(), t.getMonth(), t.getDate()) // strip time
  }, [])

  const [cursor, setCursor] = useState({ year: today.getFullYear(), month: today.getMonth() + 1 })

  const { data } = useQuery({
    queryKey: ['calendar', cursor.year, cursor.month, projectId ?? null],
    queryFn: () =>
      api
        .get<CalendarFeed>('/api/calendar', {
          params: { year: cursor.year, month: cursor.month, projectId },
        })
        .then((r) => r.data),
  })

  const firstDay = new Date(cursor.year, cursor.month - 1, 1)
  const lastDay = new Date(cursor.year, cursor.month, 0)
  const firstWeekdayOffset = (firstDay.getDay() + 6) % 7 // Monday-first
  const totalCells = Math.ceil((firstWeekdayOffset + lastDay.getDate()) / 7) * 7
  const monthLabel = `${MONTH_NAMES[cursor.month - 1]} ${cursor.year}`

  // Group tasks by day-of-month for cheap lookup
  const tasksByDay = useMemo(() => {
    const m = new Map<number, CalendarTask[]>()
    if (!data) return m
    for (const t of data.tasks) {
      const d = new Date(t.dueDate)
      const day = d.getDate()
      const arr = m.get(day) ?? []
      arr.push(t)
      m.set(day, arr)
    }
    return m
  }, [data])

  const goPrev = () => {
    const d = new Date(cursor.year, cursor.month - 2, 1)
    setCursor({ year: d.getFullYear(), month: d.getMonth() + 1 })
  }
  const goNext = () => {
    const d = new Date(cursor.year, cursor.month, 1)
    setCursor({ year: d.getFullYear(), month: d.getMonth() + 1 })
  }

  // Build grid rows
  const cells: React.ReactNode[] = []
  for (let i = 0; i < totalCells; i++) {
    const dayNumber = i - firstWeekdayOffset + 1
    if (dayNumber < 1 || dayNumber > lastDay.getDate()) {
      cells.push(<td key={i} className="calendar-cell calendar-empty"></td>)
      continue
    }
    const date = new Date(cursor.year, cursor.month - 1, dayNumber)
    const isToday = date.getTime() === today.getTime()
    const tasks = tasksByDay.get(dayNumber) ?? []
    const hasTasks = tasks.length > 0
    const cellClass =
      'calendar-cell' +
      (isToday ? ' calendar-today' : '') +
      (hasTasks ? ' calendar-has-tasks' : '')

    cells.push(
      <td key={i} className={cellClass}>
        <div className="calendar-day">{dayNumber}</div>
        {hasTasks && (
          <ul className="calendar-tasks">
            {tasks.slice(0, 3).map((t) => (
              <li
                key={t.id}
                className={`calendar-task status-${t.status.toLowerCase()}`}
                title={`${t.title}${t.projectName ? ' (' + t.projectName + ')' : ''}`}
              >
                <a href={`/Tasks/Details/${t.id}`}>{t.title}</a>
              </li>
            ))}
            {tasks.length > 3 && (
              <li className="text-muted small">+{tasks.length - 3} more</li>
            )}
          </ul>
        )}
      </td>
    )
  }

  // Wrap cells in <tr>s of 7
  const rows: React.ReactNode[] = []
  for (let i = 0; i < cells.length; i += 7) {
    rows.push(<tr key={i}>{cells.slice(i, i + 7)}</tr>)
  }

  return (
    <div className="card calendar-card mb-4">
      <div className="card-header d-flex align-items-center justify-content-between">
        <button className="btn btn-sm btn-outline-secondary" onClick={goPrev} aria-label="previous month">
          &laquo;
        </button>
        <strong>{monthLabel}</strong>
        <button className="btn btn-sm btn-outline-secondary" onClick={goNext} aria-label="next month">
          &raquo;
        </button>
      </div>
      <div className="card-body p-2">
        <table className="table table-sm calendar-grid mb-0">
          <thead>
            <tr>
              {DAYS.map((d) => (
                <th key={d} className="text-center text-muted">
                  {d}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>{rows}</tbody>
        </table>
      </div>
      <div className="card-footer small text-muted">
        <span className="legend-dot legend-has-tasks"></span> day with tasks
        &nbsp; <span className="legend-dot legend-today"></span> today
      </div>
    </div>
  )
}
