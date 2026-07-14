import { useState, useEffect, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "react-oidc-context";
import { listMissions, changeMissionStatus, ApiError } from "../../services/missionsApi";
import type { MissionListItem, GetMissionsParams } from "../../types/mission";

const DIFFICULTY_OPTIONS = [
  { value: "", label: "Todas las dificultades" },
  { value: "Easy",   label: "Fácil" },
  { value: "Medium", label: "Media" },
  { value: "Hard",   label: "Difícil" },
];

const STATUS_OPTIONS = [
  { value: "", label: "Todos los estados" },
  { value: "Draft",    label: "Borrador" },
  { value: "Active",   label: "Activa" },
  { value: "Inactive", label: "Inactiva" },
];

const DIFFICULTY_LABELS: Record<string, string> = { Easy: "Fácil", Medium: "Media", Hard: "Difícil" };
const STATUS_LABELS: Record<string, string>     = { Active: "Activa", Draft: "Borrador", Inactive: "Inactiva" };
const TYPE_LABELS: Record<string, string>       = { Treasure: "Búsqueda del Tesoro", Trivia: "Trivia" };

function difficultyClass(d: string) {
  if (d === "Easy")   return "badge badge-success";
  if (d === "Medium") return "badge badge-warning";
  if (d === "Hard")   return "badge badge-error";
  return "badge badge-muted";
}

function statusClass(s: string) {
  if (s === "Active")   return "badge badge-info";
  if (s === "Inactive") return "badge badge-error";
  return "badge badge-muted";
}

function typeClass(t: string) {
  if (t === "Treasure") return "badge badge-accent";
  if (t === "Trivia")   return "badge badge-operator";
  return "badge badge-muted";
}

export function CatalogoMisiones() {
  const auth = useAuth();
  const isAdmin = auth.user?.access_token ? (() => {
    try {
      const p = JSON.parse(atob(auth.user.access_token.split(".")[1]));
      return p.realm_access?.roles?.includes("admin") ?? false;
    } catch { return false; }
  })() : false;

  const [items, setItems]     = useState<MissionListItem[]>([]);
  const [totalCount, setTotal] = useState(0);
  const [page, setPage]        = useState(1);
  const [pageSize]             = useState(12);
  const [search, setSearch]    = useState("");
  const [difficulty, setDiff]  = useState("");
  const [status, setStatus]    = useState("");
  const [loading, setLoading]  = useState(false);
  const [error, setError]      = useState<string | null>(null);

  const totalPages = Math.ceil(totalCount / pageSize);

  async function loadMissions() {
    setLoading(true);
    setError(null);
    try {
      const params: GetMissionsParams = { page, pageSize };
      if (search.trim())  params.search     = search.trim();
      if (difficulty)     params.difficulty = difficulty;
      if (status)         params.status     = status;
      const result = await listMissions(params);
      setItems(result.items);
      setTotal(result.totalCount);
    } catch (err) {
      setError(err instanceof ApiError
        ? `Error ${err.status}: No se pudo cargar el catálogo.`
        : "Error de conexión.");
    } finally {
      setLoading(false);
    }
  }

  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(() => { loadMissions(); }, [page, pageSize, difficulty, status]);

  function handleSearchSubmit(e: FormEvent) {
    e.preventDefault();
    setPage(1);
    loadMissions();
  }

  async function toggleStatus(item: MissionListItem) {
    const newStatus = item.status === "Active" ? "Inactive" : "Active";
    try {
      await changeMissionStatus(item.id, newStatus);
      loadMissions();
    } catch (e) {
      let msg = "Error al cambiar estado";
      if (e instanceof ApiError) {
        try { const j = JSON.parse(e.body); msg = j.message || j.error || e.body; } catch { msg = e.body; }
      }
      alert(msg);
    }
  }

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1 className="page-title">Catálogo de Misiones</h1>
          <p className="page-subtitle">
            {totalCount} misión{totalCount !== 1 ? "es" : ""} registrada{totalCount !== 1 ? "s" : ""}
          </p>
        </div>
        {isAdmin && (
          <Link to="/admin/misiones/crear" className="btn btn-primary">
            + Nueva Misión
          </Link>
        )}
      </div>

      <form onSubmit={handleSearchSubmit}>
        <div className="filter-row">
          <div className="filter-group">
            <label className="form-label" htmlFor="search">Buscar</label>
            <input
              id="search" type="text" className="form-input"
              value={search} onChange={(e) => setSearch(e.target.value)}
              placeholder="Título de la misión..."
            />
          </div>
          <div className="filter-group">
            <label className="form-label" htmlFor="difficulty">Dificultad</label>
            <select id="difficulty" className="form-select" value={difficulty}
              onChange={(e) => { setDiff(e.target.value); setPage(1); }}
            >
              {DIFFICULTY_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
            </select>
          </div>
          <div className="filter-group">
            <label className="form-label" htmlFor="status">Estado</label>
            <select id="status" className="form-select" value={status}
              onChange={(e) => { setStatus(e.target.value); setPage(1); }}
            >
              {STATUS_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
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
          <div style={{ fontSize: "2.5rem", marginBottom: "1rem" }}>🗺️</div>
          <p>No se encontraron misiones</p>
        </div>
      ) : (
        <>
          <div className="mission-cards">
            {items.map(item => (
              <div key={item.id} className="mission-card">
                <div className="mission-card-badges">
                  <span className={statusClass(item.status)}>
                    {STATUS_LABELS[item.status] ?? item.status}
                  </span>
                  <span className={typeClass(item.type)}>
                    {TYPE_LABELS[item.type] ?? item.type}
                  </span>
                  <span className={difficultyClass(item.difficulty)}>
                    {DIFFICULTY_LABELS[item.difficulty] ?? item.difficulty}
                  </span>
                </div>

                <div className="mission-card-title">{item.title}</div>

                {isAdmin && (
                  <div className="mission-card-footer">
                    <Link to={`/admin/misiones/${item.id}`} className="btn btn-ghost btn-sm">
                      Ver
                    </Link>
                    <Link to={`/admin/misiones/${item.id}/editar`} className="btn btn-secondary btn-sm">
                      Editar
                    </Link>
                    <button
                      className={`btn btn-sm ${item.status === "Active" ? "btn-danger" : "btn-success"}`}
                      onClick={() => toggleStatus(item)}
                    >
                      {item.status === "Active" ? "Desactivar" : "Activar"}
                    </button>
                  </div>
                )}
              </div>
            ))}
          </div>

          {totalCount > 0 && (
            <div className="pagination">
              <span>Página {page} de {totalPages || 1}</span>
              <div className="pagination-controls">
                <button className="btn btn-ghost btn-sm" onClick={() => setPage(p => p - 1)} disabled={page <= 1}>
                  ← Anterior
                </button>
                <button className="btn btn-ghost btn-sm" onClick={() => setPage(p => p + 1)} disabled={page >= totalPages}>
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
