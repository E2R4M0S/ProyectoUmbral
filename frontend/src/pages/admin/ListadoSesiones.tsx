import { useState, useEffect } from "react";
import { listSessions, transitionSession } from "../../services/sessionsApi";
import { Link, useLocation, useNavigate } from "react-router-dom";
import type { SessionListItem, GetSessionsParams, SessionStatus } from "../../types/session";

const STATUS_OPTIONS = [
  { value: "", label: "Todos los estados" },
  { value: "Scheduled",  label: "Programada" },
  { value: "Preparing",  label: "En Preparación" },
  { value: "Active",     label: "Activa" },
  { value: "Paused",     label: "Pausada" },
  { value: "Finished",   label: "Finalizada" },
  { value: "Cancelled",  label: "Cancelada" },
];

const STATUS_LABELS: Record<string, string> = {
  Scheduled: "Programada", Preparing: "En Preparación",
  Active: "Activa", Paused: "Pausada",
  Finished: "Finalizada", Cancelled: "Cancelada",
};

const TYPE_LABELS: Record<string, string> = {
  Treasure: "Búsqueda", Trivia: "Trivia",
};

function statusClass(s: string) {
  if (s === "Active")    return "badge badge-success";
  if (s === "Preparing") return "badge badge-warning";
  if (s === "Paused")    return "badge badge-warning";
  if (s === "Finished")  return "badge badge-info";
  if (s === "Cancelled") return "badge badge-error";
  return "badge badge-muted";
}

function getTransitions(status: SessionStatus): string[] {
  switch (status) {
    case "Scheduled":  return ["Preparing", "Cancelled"];
    case "Preparing":  return ["Active", "Cancelled"];
    case "Active":     return ["Paused", "Finished", "Cancelled"];
    case "Paused":     return ["Active", "Finished", "Cancelled"];
    default:           return [];
  }
}

const TRANSITION_LABELS: Record<string, string> = {
  Preparing: "Preparar", Active: "Iniciar", Paused: "Pausar",
  Finished: "Finalizar", Cancelled: "Cancelar",
};

function transitionClass(t: string) {
  if (t === "Active")                        return "btn btn-success btn-sm";
  if (t === "Cancelled" || t === "Finished") return "btn btn-danger btn-sm";
  return "btn btn-secondary btn-sm";
}

function timeAgo(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const m = Math.floor(diff / 60000);
  if (m < 1)   return "ahora";
  if (m < 60)  return `hace ${m} min`;
  const h = Math.floor(m / 60);
  if (h < 24)  return `hace ${h} h`;
  const d = Math.floor(h / 24);
  return `hace ${d} día${d !== 1 ? "s" : ""}`;
}

function SessionCard({ item, basePath, onReload }: { item: SessionListItem; basePath: string; onReload: () => void }) {
  const navigate    = useNavigate();
  const transitions = getTransitions(item.status as SessionStatus);
  const panelUrl    = `${basePath}/sesiones/${item.id}/panel`;

  return (
    <div
      className="session-card"
      style={{ cursor: "pointer" }}
      onClick={() => navigate(panelUrl)}
    >
      <div className="session-card-top">
        <span className={statusClass(item.status)}>
          {STATUS_LABELS[item.status] ?? item.status}
        </span>
        <span className="pin-tag">{item.pin}</span>
      </div>

      <div className="session-card-title">{item.name}</div>

      <div className="session-card-mission">
        <span>{item.missionTitle}</span>
        {item.missionType && (
          <span className="badge badge-muted">{TYPE_LABELS[item.missionType] ?? item.missionType}</span>
        )}
      </div>

      <div className="session-card-stats">
        {item.stageCount > 0 && (
          <span className="session-card-stat">
            📋 {item.stageCount} etapa{item.stageCount !== 1 ? "s" : ""}
          </span>
        )}
        <span className="session-card-stat">
          👥 {item.participantCount ?? 0} participante{(item.participantCount ?? 0) !== 1 ? "s" : ""}
        </span>
        <span className="session-card-stat">🕐 {timeAgo(item.createdAt)}</span>
      </div>

      <div className="session-card-footer" onClick={(e) => e.stopPropagation()}>
        <div className="session-card-actions">
          {transitions.map(t => {
            const noParticipants = t === "Active" && (item.participantCount ?? 0) === 0;
            return (
              <button
                key={t}
                className={transitionClass(t)}
                disabled={noParticipants}
                title={noParticipants ? "No hay participantes en la sesión" : undefined}
                onClick={async () => {
                  try { await transitionSession(item.id, t); onReload(); } catch { /* ignore */ }
                }}
              >
                {TRANSITION_LABELS[t] ?? t}
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
}

export function ListadoSesiones() {
  const location = useLocation();
  const basePath = location.pathname.startsWith("/admin") ? "/admin" : "/operator";

  const [items, setItems]      = useState<SessionListItem[]>([]);
  const [totalCount, setTotal] = useState(0);
  const [page, setPage]        = useState(1);
  const [search, setSearch]    = useState("");
  const [status, setStatus]    = useState("");
  const [loading, setLoading]  = useState(false);
  const [error, setError]      = useState<string | null>(null);

  const pageSize   = 12;
  const totalPages = Math.ceil(totalCount / pageSize);

  async function loadSessions() {
    setLoading(true);
    setError(null);
    try {
      const params: GetSessionsParams = { page, pageSize };
      if (search.trim()) params.search = search.trim();
      if (status)        params.status  = status;
      const r = await listSessions(params);
      setItems(r.items);
      setTotal(r.totalCount);
    } catch {
      setError("Error al cargar las sesiones.");
    } finally {
      setLoading(false);
    }
  }

  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(() => { loadSessions(); }, [page, status]);

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1 className="page-title">Sesiones de Juego</h1>
          <p className="page-subtitle">
            {totalCount} sesión{totalCount !== 1 ? "es" : ""} registrada{totalCount !== 1 ? "s" : ""}
          </p>
        </div>
        <Link to={`${basePath}/sesiones/crear`} className="btn btn-primary">
          + Nueva Sesión
        </Link>
      </div>

      <form onSubmit={(e) => { e.preventDefault(); setPage(1); loadSessions(); }}>
        <div className="filter-row">
          <div className="filter-group">
            <label className="form-label" htmlFor="search">Buscar</label>
            <input
              id="search"
              type="text"
              className="form-input"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Nombre de la sesión..."
            />
          </div>
          <div className="filter-group">
            <label className="form-label" htmlFor="status">Estado</label>
            <select
              id="status"
              className="form-select"
              value={status}
              onChange={(e) => { setStatus(e.target.value); setPage(1); }}
            >
              {STATUS_OPTIONS.map(o => (
                <option key={o.value} value={o.value}>{o.label}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="form-label" style={{ visibility: "hidden" }}>.</label>
            <button type="submit" className="btn btn-secondary">Buscar</button>
          </div>
        </div>
      </form>

      {error && <div className="alert alert-error">{error}</div>}

      {loading ? (
        <div style={{ textAlign: "center", padding: "3rem" }}>
          <div className="spinner" style={{ margin: "0 auto" }} />
        </div>
      ) : items.length === 0 ? (
        <div style={{ textAlign: "center", padding: "4rem", color: "var(--text-muted)" }}>
          <div style={{ fontSize: "2.5rem", marginBottom: "1rem" }}>📋</div>
          <p>No se encontraron sesiones</p>
        </div>
      ) : (
        <>
          <div className="session-cards">
            {items.map(item => (
              <SessionCard
                key={item.id}
                item={item}
                basePath={basePath}
                onReload={loadSessions}
              />
            ))}
          </div>

          {totalPages > 1 && (
            <div className="pagination">
              <span>Página {page} de {totalPages}</span>
              <div className="pagination-controls">
                <button
                  className="btn btn-ghost btn-sm"
                  onClick={() => setPage(p => p - 1)}
                  disabled={page <= 1}
                >
                  ← Anterior
                </button>
                <button
                  className="btn btn-ghost btn-sm"
                  onClick={() => setPage(p => p + 1)}
                  disabled={page >= totalPages}
                >
                  Siguiente →
                </button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}
