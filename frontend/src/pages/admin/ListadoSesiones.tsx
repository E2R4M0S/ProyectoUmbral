import { useState, useEffect, type FormEvent } from "react";
import { listSessions, ApiError } from "../../services/sessionsApi";
import type { SessionListItem, GetSessionsParams } from "../../types/session";

const STATUS_OPTIONS = [
  { value: "", label: "Todos los estados" },
  { value: "Scheduled", label: "Programada" },
  { value: "Preparing", label: "Preparándose" },
  { value: "Active", label: "Activa" },
  { value: "Paused", label: "Pausada" },
  { value: "Finished", label: "Finalizada" },
  { value: "Cancelled", label: "Cancelada" },
];

const tableStyle: React.CSSProperties = {
  width: "100%",
  borderCollapse: "collapse",
  fontSize: 14,
};

const thStyle: React.CSSProperties = {
  textAlign: "left",
  padding: "10px 12px",
  borderBottom: "2px solid #e94560",
  backgroundColor: "#16213e",
  fontWeight: 600,
  color: "white",
};

const tdStyle: React.CSSProperties = {
  padding: "10px 12px",
  borderBottom: "1px solid #0f3460",
  verticalAlign: "middle",
  color: "white",
};

const inputStyle: React.CSSProperties = {
  width: "100%",
  padding: 8,
  border: "1px solid #0f3460",
  borderRadius: 4,
  boxSizing: "border-box",
  backgroundColor: "#16213e",
  color: "white",
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
  backgroundColor: primary ? "#0f3460" : "#6c757d",
  color: "#fff",
});

const errorStyle: React.CSSProperties = {
  padding: "10px 14px",
  border: "1px solid #e94560",
  borderRadius: 4,
  backgroundColor: "#2d1a1a",
  color: "#e94560",
  marginBottom: 16,
  fontSize: 14,
};

const containerStyle: React.CSSProperties = {
  padding: "1rem",
  maxWidth: 1200,
  margin: "0 auto",
};

const badgeStyle = (status: string): React.CSSProperties => {
  let bgColor = "#6c757d";
  switch (status) {
    case "Scheduled": bgColor = "#ffc107"; break;
    case "Preparing": bgColor = "#fd7e14"; break;
    case "Active": bgColor = "#28a745"; break;
    case "Paused": bgColor = "#6f42c1"; break;
    case "Finished": bgColor = "#007bff"; break;
    case "Cancelled": bgColor = "#dc3545"; break;
  }
  return {
    display: "inline-block",
    padding: "2px 8px",
    borderRadius: 12,
    fontSize: 12,
    fontWeight: 600,
    color: "#fff",
    backgroundColor: bgColor,
  };
};

const statusLabel = (status: string): string => {
  switch (status) {
    case "Scheduled": return "Programada";
    case "Preparing": return "Preparándose";
    case "Active": return "Activa";
    case "Paused": return "Pausada";
    case "Finished": return "Finalizada";
    case "Cancelled": return "Cancelada";
    default: return status;
  }
};

export function ListadoSesiones() {
  const [items, setItems] = useState<SessionListItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, _setPageSize] = useState(10);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const totalPages = Math.ceil(totalCount / pageSize);

  async function loadSessions() {
    setLoading(true);
    setError(null);
    try {
      const params: GetSessionsParams = { page, pageSize };
      if (search.trim()) params.search = search.trim();
      if (status) params.status = status;

      const result = await listSessions(params);
      setItems(result.items);
      setTotalCount(result.totalCount);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(`Error ${err.status}: No se pudo cargar el listado de sesiones.`);
      } else {
        setError("Error de conexión. Verificá tu conexión a internet.");
      }
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadSessions();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize]);

  function handleSearchSubmit(e: FormEvent) {
    e.preventDefault();
    setPage(1);
    loadSessions();
  }

  function handleFilterChange() {
    setPage(1);
    loadSessions();
  }

  function prevPage() {
    if (page > 1) setPage((p) => p - 1);
  }

  function nextPage() {
    if (page < totalPages) setPage((p) => p + 1);
  }

  return (
    <div style={containerStyle}>
      <h2 style={{ marginBottom: "1rem" }}>Listado de Sesiones</h2>

      {/* Filters */}
      <form onSubmit={handleSearchSubmit} style={{ marginBottom: 16 }}>
        <div style={filterRowStyle}>
          <div style={filterGroupStyle}>
            <label htmlFor="search" style={labelStyle}>Buscar</label>
            <input
              id="search"
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Nombre de la sesión..."
              style={inputStyle}
            />
          </div>

          <div style={filterGroupStyle}>
            <label htmlFor="status" style={labelStyle}>Estado</label>
            <select
              id="status"
              value={status}
              onChange={(e) => {
                setStatus(e.target.value);
                handleFilterChange();
              }}
              style={{ ...inputStyle, cursor: "pointer" }}
            >
              {STATUS_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>{o.label}</option>
              ))}
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
      {loading && <div style={{ marginBottom: 16, color: "#aaa" }}>Cargando...</div>}

      {/* Table */}
      {!loading && (
        <>
          <div style={{ marginBottom: 8, fontSize: 13, color: "#aaa" }}>
            {totalCount === 0
              ? "Sin resultados"
              : `${totalCount} sesión${totalCount !== 1 ? "es" : ""} encontrada${totalCount !== 1 ? "s" : ""}`}
          </div>

          <table style={tableStyle}>
            <thead>
              <tr>
                <th style={thStyle}>Nombre</th>
                <th style={thStyle}>Misión</th>
                <th style={thStyle}>Estado</th>
                <th style={thStyle}>PIN</th>
                <th style={thStyle}>Fecha</th>
              </tr>
            </thead>
            <tbody>
              {items.length === 0 ? (
                <tr>
                  <td colSpan={5} style={{ ...tdStyle, textAlign: "center", color: "#666" }}>
                    No se encontraron sesiones
                  </td>
                </tr>
              ) : (
                items.map((item) => (
                  <tr key={item.id}>
                    <td style={tdStyle}>{item.name}</td>
                    <td style={tdStyle}>{item.missionTitle}</td>
                    <td style={tdStyle}>
                      <span style={badgeStyle(item.status)}>
                        {statusLabel(item.status)}
                      </span>
                    </td>
                    <td style={tdStyle}>
                      <code style={{
                        padding: "2px 8px",
                        backgroundColor: "#0f3460",
                        borderRadius: 4,
                      }}>
                        {item.pin}
                      </code>
                    </td>
                    <td style={tdStyle}>
                      {new Date(item.createdAt).toLocaleDateString("es-AR")}
                    </td>
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