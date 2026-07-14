import { useState, useEffect, type FormEvent } from "react";
import { useAuth } from "react-oidc-context";
import { useNavigate } from "react-router-dom";
import { getUsers, ApiError } from "../../services/adminUsuariosApi";
import type { UserListItem, GetUsersParams } from "../../types/usuario";

const ROLE_OPTIONS = [
  { value: "", label: "Todos los roles" },
  { value: "admin",       label: "Administrador" },
  { value: "operator",    label: "Operador" },
  { value: "participant", label: "Participante" },
];

function getRoles(token: string): string[] {
  try { return JSON.parse(atob(token.split(".")[1])).realm_access?.roles ?? []; }
  catch { return []; }
}

function formatDate(d: string) {
  if (!d) return "—";
  return new Date(d).toLocaleDateString("es", { day: "2-digit", month: "short", year: "numeric" });
}

function roleClass(r: string) {
  if (r === "admin")       return "badge badge-admin";
  if (r === "operator")    return "badge badge-operator";
  if (r === "participant") return "badge badge-participant";
  return "badge badge-muted";
}

function roleLabel(r: string) {
  if (r === "admin")       return "Admin";
  if (r === "operator")    return "Operador";
  if (r === "participant") return "Participante";
  return r;
}

export function ListadoUsuarios() {
  const auth     = useAuth();
  const navigate = useNavigate();
  const isAdmin  = auth.user?.access_token ? getRoles(auth.user.access_token).includes("admin") : false;

  const [items, setItems]      = useState<UserListItem[]>([]);
  const [totalCount, setTotal] = useState(0);
  const [page, setPage]        = useState(1);
  const [pageSize]             = useState(20);
  const [search, setSearch]    = useState("");
  const [role, setRole]        = useState("");
  const [enabled, setEnabled]  = useState<boolean | undefined>(undefined);
  const [loading, setLoading]  = useState(false);
  const [error, setError]      = useState<string | null>(null);

  const totalPages = Math.ceil(totalCount / pageSize);

  async function loadUsers() {
    setLoading(true);
    setError(null);
    try {
      const params: GetUsersParams = { page, pageSize };
      if (search.trim())         params.search  = search.trim();
      if (role)                  params.role    = role;
      if (enabled !== undefined) params.enabled = enabled;
      const result = await getUsers(params);
      setItems(result.items);
      setTotal(result.totalCount);
    } catch (err) {
      setError(err instanceof ApiError
        ? `Error ${err.status}: No se pudo cargar el listado.`
        : "Error de conexión.");
    } finally {
      setLoading(false);
    }
  }

  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(() => { loadUsers(); }, [page, role, enabled]);

  function handleSearchSubmit(e: FormEvent) {
    e.preventDefault();
    setPage(1);
    loadUsers();
  }

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1 className="page-title">Listado de Usuarios</h1>
          <p className="page-subtitle">
            {totalCount} usuario{totalCount !== 1 ? "s" : ""} registrado{totalCount !== 1 ? "s" : ""}
          </p>
        </div>
      </div>

      <form onSubmit={handleSearchSubmit}>
        <div className="filter-row">
          <div className="filter-group">
            <label className="form-label" htmlFor="search">Buscar</label>
            <input
              id="search" type="text" className="form-input"
              value={search} onChange={(e) => setSearch(e.target.value)}
              placeholder="Nombre o email..."
            />
          </div>
          {isAdmin && (
            <div className="filter-group">
              <label className="form-label" htmlFor="role">Rol</label>
              <select id="role" className="form-select" value={role}
                onChange={(e) => { setRole(e.target.value); setPage(1); }}
              >
                {ROLE_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
              </select>
            </div>
          )}
          <div className="filter-group">
            <label className="form-label" htmlFor="enabled">Estado</label>
            <select id="enabled" className="form-select"
              value={enabled === undefined ? "" : enabled ? "true" : "false"}
              onChange={(e) => {
                const v = e.target.value;
                setEnabled(v === "" ? undefined : v === "true");
                setPage(1);
              }}
            >
              <option value="">Todos</option>
              <option value="true">Activos</option>
              <option value="false">Inactivos</option>
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
          <div style={{ fontSize: "2.5rem", marginBottom: "1rem" }}>👤</div>
          <p>No se encontraron usuarios</p>
        </div>
      ) : (
        <>
          <div className="user-cards">
            {items.map(item => (
              <div
                key={item.id}
                className="user-card"
                onClick={() => navigate(`/admin/usuarios/${item.id}`)}
              >
                <div className="user-card-header">
                  <div className="user-card-avatar">
                    {(item.name || "?")[0].toUpperCase()}
                  </div>
                  <div>
                    <div className="user-card-name">{item.name}</div>
                    <div className="user-card-email">{item.email}</div>
                  </div>
                </div>

                <div className="user-card-footer">
                  <div className="user-card-roles">
                    {item.roles.map(r => (
                      <span key={r} className={roleClass(r)}>{roleLabel(r)}</span>
                    ))}
                  </div>
                  <div style={{ display: "flex", alignItems: "center", gap: "var(--space-2)" }}>
                    <span className={item.enabled ? "badge badge-success" : "badge badge-error"}>
                      {item.enabled ? "Activo" : "Inactivo"}
                    </span>
                    <span className="user-card-date">{formatDate(item.createdAt)}</span>
                  </div>
                </div>
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
