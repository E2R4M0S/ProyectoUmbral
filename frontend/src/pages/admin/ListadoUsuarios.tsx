import { useState, useEffect, type FormEvent } from "react";
import { getUsers, ApiError } from "../../services/adminUsuariosApi";
import type { UserListItem, GetUsersParams } from "../../types/usuario";

const ROLE_OPTIONS = [
  { value: "", label: "Todos los roles" },
  { value: "admin", label: "Administrador" },
  { value: "operator", label: "Operador" },
  { value: "participant", label: "Participante" },
];

const inputStyle: React.CSSProperties = {
  width: "100%",
  padding: 8,
  border: "1px solid #ccc",
  borderRadius: 4,
  boxSizing: "border-box",
  fontSize: 14,
};

const labelStyle: React.CSSProperties = {
  display: "block",
  marginBottom: 4,
  fontWeight: 600,
  fontSize: 13,
};

const filterRowStyle: React.CSSProperties = {
  display: "flex",
  gap: 12,
  marginBottom: 16,
  alignItems: "flex-end",
  flexWrap: "wrap",
};

const filterGroupStyle: React.CSSProperties = {
  flex: "1 1 180px",
  minWidth: 150,
};

const tableStyle: React.CSSProperties = {
  width: "100%",
  borderCollapse: "collapse",
  fontSize: 14,
};

const thStyle: React.CSSProperties = {
  textAlign: "left",
  padding: "10px 12px",
  borderBottom: "2px solid #dee2e6",
  backgroundColor: "#f8f9fa",
  fontWeight: 600,
  color: "#495057",
};

const tdStyle: React.CSSProperties = {
  padding: "10px 12px",
  borderBottom: "1px solid #dee2e6",
  verticalAlign: "middle",
};

const badgeStyle = (color: string): React.CSSProperties => ({
  display: "inline-block",
  padding: "2px 8px",
  borderRadius: 12,
  fontSize: 12,
  fontWeight: 600,
  color: "#fff",
  backgroundColor: color,
});

const enabledStyle = badgeStyle("#28a745");
const disabledStyle = badgeStyle("#dc3545");

const paginationStyle: React.CSSProperties = {
  display: "flex",
  justifyContent: "space-between",
  alignItems: "center",
  marginTop: 16,
  fontSize: 14,
};

const buttonStyle = (primary: boolean): React.CSSProperties => ({
  padding: "6px 14px",
  border: "none",
  borderRadius: 4,
  cursor: "pointer",
  fontSize: 13,
  fontWeight: 600,
  backgroundColor: primary ? "#007bff" : "#6c757d",
  color: "#fff",
});

const errorStyle: React.CSSProperties = {
  padding: "10px 14px",
  border: "1px solid #dc3545",
  borderRadius: 4,
  backgroundColor: "#fff5f5",
  color: "#dc3545",
  marginBottom: 16,
  fontSize: 14,
};

function formatDate(dateStr: string): string {
  if (!dateStr) return "—";
  const d = new Date(dateStr);
  return d.toLocaleDateString("es-AR", { day: "2-digit", month: "short", year: "numeric" });
}

export function ListadoUsuarios() {
  const [items, setItems] = useState<UserListItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, _setPageSize] = useState(20);
  const [search, setSearch] = useState("");
  const [role, setRole] = useState("");
  const [enabled, setEnabled] = useState<boolean | undefined>(undefined);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const totalPages = Math.ceil(totalCount / pageSize);

  async function loadUsers() {
    setLoading(true);
    setError(null);
    try {
      const params: GetUsersParams = { page, pageSize };
      if (search.trim()) params.search = search.trim();
      if (role) params.role = role;
      if (enabled !== undefined) params.enabled = enabled;

      const result = await getUsers(params);
      setItems(result.items);
      setTotalCount(result.totalCount);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(`Error ${err.status}: No se pudo cargar el listado de usuarios.`);
      } else {
        setError("Error de conexión. Verificá tu conexión a internet.");
      }
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadUsers();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize]);

  function handleSearchSubmit(e: FormEvent) {
    e.preventDefault();
    setPage(1);
    loadUsers();
  }

  function handleFilterChange() {
    setPage(1);
    loadUsers();
  }

  function prevPage() {
    if (page > 1) setPage(p => p - 1);
  }

  function nextPage() {
    if (page < totalPages) setPage(p => p + 1);
  }

  return (
    <div style={{ padding: "1rem", maxWidth: 1200, margin: "0 auto" }}>
      <h2 style={{ marginBottom: "1rem" }}>Listado de Usuarios</h2>

      {/* Filters */}
      <form onSubmit={handleSearchSubmit} style={{ marginBottom: 16 }}>
        <div style={filterRowStyle}>
          <div style={filterGroupStyle}>
            <label htmlFor="search" style={labelStyle}>Buscar</label>
            <input
              id="search"
              type="text"
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder="Nombre o email..."
              style={inputStyle}
            />
          </div>

          <div style={filterGroupStyle}>
            <label htmlFor="role" style={labelStyle}>Rol</label>
            <select
              id="role"
              value={role}
              onChange={e => { setRole(e.target.value); handleFilterChange(); }}
              style={{ ...inputStyle, cursor: "pointer" }}
            >
              {ROLE_OPTIONS.map(o => (
                <option key={o.value} value={o.value}>{o.label}</option>
              ))}
            </select>
          </div>

          <div style={filterGroupStyle}>
            <label htmlFor="enabled" style={labelStyle}>Estado</label>
            <select
              id="enabled"
              value={enabled === undefined ? "" : enabled ? "true" : "false"}
              onChange={e => {
                const v = e.target.value;
                setEnabled(v === "" ? undefined : v === "true");
                handleFilterChange();
              }}
              style={{ ...inputStyle, cursor: "pointer" }}
            >
              <option value="">Todos</option>
              <option value="true">Activos</option>
              <option value="false">Inactivos</option>
            </select>
          </div>

          <div style={{ flex: "0 0 auto" }}>
            <button type="submit" style={{ ...buttonStyle(true), marginTop: 20 }}>
              Buscar
            </button>
          </div>
        </div>
      </form>

      {/* Error */}
      {error && <div style={errorStyle}>{error}</div>}

      {/* Loading */}
      {loading && <div style={{ marginBottom: 16, color: "#666" }}>Cargando...</div>}

      {/* Table */}
      {!loading && (
        <>
          <div style={{ marginBottom: 8, fontSize: 13, color: "#666" }}>
            {totalCount === 0
              ? "Sin resultados"
              : `${totalCount} usuario${totalCount !== 1 ? "s" : ""} encontrado${totalCount !== 1 ? "s" : ""}`}
          </div>

          <table style={tableStyle}>
            <thead>
              <tr>
                <th style={thStyle}>Nombre</th>
                <th style={thStyle}>Email</th>
                <th style={thStyle}>Roles</th>
                <th style={thStyle}>Estado</th>
                <th style={thStyle}>Creado</th>
              </tr>
            </thead>
            <tbody>
              {items.length === 0 ? (
                <tr>
                  <td colSpan={5} style={{ ...tdStyle, textAlign: "center", color: "#666" }}>
                    No se encontraron usuarios
                  </td>
                </tr>
              ) : (
                items.map(item => (
                  <tr key={item.id}>
                    <td style={tdStyle}>{item.name}</td>
                    <td style={tdStyle}>{item.email}</td>
                    <td style={tdStyle}>
                      {item.roles.map(r => (
                        <span
                          key={r}
                          style={badgeStyle(r === "admin" ? "#6610f2" : r === "operator" ? "#17a2b8" : "#28a745")}
                        >
                          {r}
                        </span>
                      ))}
                    </td>
                    <td style={tdStyle}>
                      <span style={item.enabled ? enabledStyle : disabledStyle}>
                        {item.enabled ? "Activo" : "Inactivo"}
                      </span>
                    </td>
                    <td style={tdStyle}>{formatDate(item.createdAt)}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>

          {/* Pagination */}
          {totalCount > 0 && (
            <div style={paginationStyle}>
              <span>
                Página {page} de {totalPages || 1} — {pageSize} por página
              </span>
              <div style={{ display: "flex", gap: 8 }}>
                <button
                  onClick={prevPage}
                  disabled={page <= 1}
                  style={buttonStyle(false)}
                >
                  Anterior
                </button>
                <button
                  onClick={nextPage}
                  disabled={page >= totalPages}
                  style={buttonStyle(false)}
                >
                  Siguiente
                </button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}