import { useState, useEffect, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "react-oidc-context";
import { listMissions, changeMissionStatus, ApiError } from "../../services/missionsApi";
import type { MissionListItem, GetMissionsParams } from "../../types/mission";

const DIFFICULTY_OPTIONS = [
  { value: "", label: "Todas las dificultades" },
  { value: "Easy", label: "Fácil" },
  { value: "Medium", label: "Media" },
  { value: "Hard", label: "Difícil" },
];

const STATUS_OPTIONS = [
  { value: "", label: "Todos los estados" },
  { value: "Draft", label: "Borrador" },
  { value: "Active", label: "Activa" },
  { value: "Inactive", label: "Inactiva" },
];

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

const DIFFICULTY_LABELS: Record<string, string> = { Easy: "Fácil", Medium: "Media", Hard: "Difícil" };
const STATUS_LABELS: Record<string, string>     = { Active: "Activa", Draft: "Borrador", Inactive: "Inactiva" };
const TYPE_LABELS: Record<string, string>       = { Treasure: "Búsqueda del Tesoro", Trivia: "Trivia" };

export function CatalogoMisiones() {
  const auth = useAuth();
  const isAdmin = auth.user?.access_token ? (() => {
    try {
      const p = JSON.parse(atob(auth.user.access_token.split(".")[1]));
      return p.realm_access?.roles?.includes("admin") ?? false;
    } catch { return false; }
  })() : false;

  const [items, setItems]         = useState<MissionListItem[]>([]);
  const [totalCount, setTotal]    = useState(0);
  const [page, setPage]           = useState(1);
  const [pageSize]                = useState(10);
  const [search, setSearch]       = useState("");
  const [difficulty, setDiff]     = useState("");
  const [status, setStatus]       = useState("");
  const [loading, setLoading]     = useState(false);
  const [error, setError]         = useState<string | null>(null);

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

  useEffect(() => { loadMissions(); }, [page, pageSize]); // eslint-disable-line

  function handleSearchSubmit(e: FormEvent) {
    e.preventDefault();
    setPage(1);
    loadMissions();
  }

  function handleFilterChange() {
    setPage(1);
    loadMissions();
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

      {/* Filters */}
      <form onSubmit={handleSearchSubmit}>
        <div className="filter-row">
          <div className="filter-group">
            <label className="form-label" htmlFor="search">Buscar</label>
            <input
              id="search"
              type="text"
              className="form-input"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Título de la misión..."
            />
          </div>
          <div className="filter-group">
            <label className="form-label" htmlFor="difficulty">Dificultad</label>
            <select
              id="difficulty"
              className="form-select"
              value={difficulty}
              onChange={(e) => { setDiff(e.target.value); handleFilterChange(); }}
            >
              {DIFFICULTY_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>{o.label}</option>
              ))}
            </select>
          </div>
          <div className="filter-group">
            <label className="form-label" htmlFor="status">Estado</label>
            <select
              id="status"
              className="form-select"
              value={status}
              onChange={(e) => { setStatus(e.target.value); handleFilterChange(); }}
            >
              {STATUS_OPTIONS.map((o) => (
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
      ) : (
        <>
          <div className="table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Título</th>
                  <th>Dificultad</th>
                  <th>Tipo</th>
                  <th>Estado</th>
                  {isAdmin && <th>Acciones</th>}
                </tr>
              </thead>
              <tbody>
                {items.length === 0 ? (
                  <tr>
                    <td colSpan={isAdmin ? 5 : 4} style={{ textAlign: "center", padding: "3rem", color: "var(--text-muted)" }}>
                      No se encontraron misiones
                    </td>
                  </tr>
                ) : (
                  items.map((item) => (
                    <tr key={item.id}>
                      <td style={{ fontWeight: 500 }}>{item.title}</td>
                      <td>
                        <span className={difficultyClass(item.difficulty)}>
                          {DIFFICULTY_LABELS[item.difficulty] ?? item.difficulty}
                        </span>
                      </td>
                      <td style={{ color: "var(--text-secondary)" }}>
                        {TYPE_LABELS[item.type] ?? item.type}
                      </td>
                      <td>
                        <span className={statusClass(item.status)}>
                          {STATUS_LABELS[item.status] ?? item.status}
                        </span>
                      </td>
                      {isAdmin && (
                        <td>
                          <div style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap" }}>
                            <Link to={`/admin/misiones/${item.id}`} className="btn btn-ghost btn-sm">
                              Ver
                            </Link>
                            <Link to={`/admin/misiones/${item.id}/editar`} className="btn btn-secondary btn-sm">
                              Editar
                            </Link>
                            <button
                              className={`btn btn-sm ${item.status === "Active" ? "btn-danger" : "btn-success"}`}
                              onClick={async () => {
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
                              }}
                            >
                              {item.status === "Active" ? "Desactivar" : "Activar"}
                            </button>
                          </div>
                        </td>
                      )}
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          {totalCount > 0 && (
            <div className="pagination">
              <span>Página {page} de {totalPages || 1}</span>
              <div className="pagination-controls">
                <button
                  className="btn btn-ghost btn-sm"
                  onClick={() => setPage((p) => p - 1)}
                  disabled={page <= 1}
                >
                  ← Anterior
                </button>
                <button
                  className="btn btn-ghost btn-sm"
                  onClick={() => setPage((p) => p + 1)}
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
