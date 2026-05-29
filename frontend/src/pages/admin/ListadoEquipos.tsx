import { useState, useEffect, type FormEvent } from "react";
import { listTeams, ApiError } from "../../services/teamsApi";
import type { TeamListItem, GetTeamsParams } from "../../types/team";

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
  color: "white",
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
  backgroundColor: "#1a1a2e",
  minHeight: "100vh",
  color: "white",
};

export function ListadoEquipos() {
  const [items, setItems] = useState<TeamListItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, _setPageSize] = useState(10);
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const totalPages = Math.ceil(totalCount / pageSize);

  async function loadTeams() {
    setLoading(true);
    setError(null);
    try {
      const params: GetTeamsParams = { page, pageSize };
      if (search.trim()) params.search = search.trim();

      const result = await listTeams(params);
      setItems(result.items);
      setTotalCount(result.totalCount);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(`Error ${err.status}: No se pudo cargar el listado de equipos.`);
      } else {
        setError("Error de conexión. Verificá tu conexión a internet.");
      }
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadTeams();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize]);

  function handleSearchSubmit(e: FormEvent) {
    e.preventDefault();
    setPage(1);
    loadTeams();
  }

  function prevPage() {
    if (page > 1) setPage((p) => p - 1);
  }

  function nextPage() {
    if (page < totalPages) setPage((p) => p + 1);
  }

  return (
    <div style={containerStyle}>
      <h2 style={{ marginBottom: "1rem" }}>Listado de Equipos</h2>

      {/* Search */}
      <form onSubmit={handleSearchSubmit} style={{ marginBottom: 16 }}>
        <div style={filterRowStyle}>
          <div style={filterGroupStyle}>
            <label htmlFor="search" style={labelStyle}>Buscar</label>
            <input
              id="search"
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Nombre del equipo..."
              style={inputStyle}
            />
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
              : `${totalCount} equipo${totalCount !== 1 ? "s" : ""} encontrado${totalCount !== 1 ? "s" : ""}`}
          </div>

          <table style={tableStyle}>
            <thead>
              <tr>
                <th style={thStyle}>Nombre</th>
                <th style={thStyle}>Miembros</th>
                <th style={thStyle}>Código de Unión</th>
                <th style={thStyle}>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {items.length === 0 ? (
                <tr>
                  <td colSpan={4} style={{ ...tdStyle, textAlign: "center", color: "#666" }}>
                    No se encontraron equipos
                  </td>
                </tr>
              ) : (
                items.map((item) => (
                  <tr key={item.id}>
                    <td style={tdStyle}>{item.name}</td>
                    <td style={tdStyle}>{item.memberCount}</td>
                    <td style={tdStyle}>
                      <code style={{
                        padding: "2px 8px",
                        backgroundColor: "#0f3460",
                        borderRadius: 4,
                        color: "white",
                      }}>
                        {item.joinCode}
                      </code>
                    </td>
                    <td style={tdStyle}>
                      <a
                        href={`/admin/equipos/${item.id}`}
                        style={{
                          ...buttonStyle(true),
                          textDecoration: "none",
                          display: "inline-block",
                        }}
                      >
                        Ver Detalle
                      </a>
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