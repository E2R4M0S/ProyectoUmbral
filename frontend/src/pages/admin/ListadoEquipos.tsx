import { Link } from "react-router-dom";
import { useState, useEffect, type FormEvent } from "react";
import { listTeams, ApiError } from "../../services/teamsApi";
import { useAuth } from "react-oidc-context";
import type { TeamListItem, GetTeamsParams } from "../../types/team";

function getBasePath(token?: string): string {
  if (!token) return "/operator";
  try {
    const p = JSON.parse(atob(token.split(".")[1]));
    return p.realm_access?.roles?.includes("admin") ? "/admin" : "/operator";
  } catch { return "/operator"; }
}

export function ListadoEquipos() {
  const auth = useAuth();
  const base = getBasePath(auth.user?.access_token);

  const [items, setItems]      = useState<TeamListItem[]>([]);
  const [totalCount, setTotal] = useState(0);
  const [page, setPage]        = useState(1);
  const [search, setSearch]    = useState("");
  const [loading, setLoading]  = useState(false);
  const [error, setError]      = useState<string | null>(null);

  const pageSize   = 12;
  const totalPages = Math.ceil(totalCount / pageSize);

  async function loadTeams() {
    setLoading(true);
    setError(null);
    try {
      const params: GetTeamsParams = { page, pageSize };
      if (search.trim()) params.search = search.trim();
      const result = await listTeams(params);
      setItems(result.items);
      setTotal(result.totalCount);
    } catch (err) {
      setError(err instanceof ApiError
        ? `Error ${err.status}: No se pudo cargar el listado de equipos.`
        : "Error de conexión. Verifica tu conexión a internet.");
    } finally {
      setLoading(false);
    }
  }

  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(() => { loadTeams(); }, [page]);

  function handleSearchSubmit(e: FormEvent) {
    e.preventDefault();
    setPage(1);
    loadTeams();
  }

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1 className="page-title">Listado de Equipos</h1>
          <p className="page-subtitle">
            {totalCount} equipo{totalCount !== 1 ? "s" : ""} registrado{totalCount !== 1 ? "s" : ""}
          </p>
        </div>
        <Link to={`${base}/equipos/crear`} className="btn btn-primary">
          + Nuevo Equipo
        </Link>
      </div>

      <form onSubmit={handleSearchSubmit}>
        <div className="filter-row">
          <div className="filter-group">
            <label className="form-label" htmlFor="search">Buscar</label>
            <input
              id="search" type="text" className="form-input"
              value={search} onChange={(e) => setSearch(e.target.value)}
              placeholder="Nombre del equipo..."
            />
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
          <div style={{ fontSize: "2.5rem", marginBottom: "1rem" }}>👥</div>
          <p>No se encontraron equipos</p>
        </div>
      ) : (
        <>
          <div className="team-cards">
            {items.map(item => (
              <div key={item.id} className="team-card">
                <div className="team-card-title">{item.name}</div>

                <div className="team-card-stats">
                  <span>👥 {item.memberCount} miembro{item.memberCount !== 1 ? "s" : ""}</span>
                  <span className="pin-tag">{item.joinCode}</span>
                </div>

                <Link to={`${base}/equipos/${item.id}`} className="btn btn-ghost btn-sm" style={{ marginTop: "auto" }}>
                  Ver detalle →
                </Link>
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
