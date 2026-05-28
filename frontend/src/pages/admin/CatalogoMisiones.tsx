import { useState, useEffect, type FormEvent } from "react";
import { listMissions, ApiError } from "../../services/missionsApi";
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

const difficultyBadgeColor = (difficulty: string): string => {
  switch (difficulty) {
    case "Easy": return "#28a745";
    case "Medium": return "#ffc107";
    case "Hard": return "#dc3545";
    default: return "#6c757d";
  }
};

const statusBadgeColor = (status: string): string => {
  switch (status) {
    case "Active": return "#007bff";
    case "Draft": return "#6c757d";
    default: return "#6c757d";
  }
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

export function CatalogoMisiones() {
  const [items, setItems] = useState<MissionListItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, _setPageSize] = useState(10);
  const [search, setSearch] = useState("");
  const [difficulty, setDifficulty] = useState("");
  const [status, setStatus] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const totalPages = Math.ceil(totalCount / pageSize);

  async function loadMissions() {
    setLoading(true);
    setError(null);
    try {
      const params: GetMissionsParams = { page, pageSize };
      if (search.trim()) params.search = search.trim();
      if (difficulty) params.difficulty = difficulty;
      if (status) params.status = status;

      const result = await listMissions(params);
      setItems(result.items);
      setTotalCount(result.totalCount);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(`Error ${err.status}: No se pudo cargar el catálogo de misiones.`);
      } else {
        setError("Error de conexión. Verificá tu conexión a internet.");
      }
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadMissions();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize]);

  function handleSearchSubmit(e: FormEvent) {
    e.preventDefault();
    setPage(1);
    loadMissions();
  }

  function handleFilterChange() {
    setPage(1);
    loadMissions();
  }

  function prevPage() {
    if (page > 1) setPage((p) => p - 1);
  }

  function nextPage() {
    if (page < totalPages) setPage((p) => p + 1);
  }

  return (
    <div style={{ padding: "1rem", maxWidth: 1200, margin: "0 auto" }}>
      <h2 style={{ marginBottom: "1rem" }}>Catálogo de Misiones</h2>

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
              placeholder="Título de la misión..."
              style={inputStyle}
            />
          </div>

          <div style={filterGroupStyle}>
            <label htmlFor="difficulty" style={labelStyle}>Dificultad</label>
            <select
              id="difficulty"
              value={difficulty}
              onChange={(e) => {
                setDifficulty(e.target.value);
                handleFilterChange();
              }}
              style={{ ...inputStyle, cursor: "pointer" }}
            >
              {DIFFICULTY_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>{o.label}</option>
              ))}
            </select>
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
      {loading && <div style={{ marginBottom: 16, color: "#666" }}>Cargando...</div>}

      {/* Table */}
      {!loading && (
        <>
          <div style={{ marginBottom: 8, fontSize: 13, color: "#666" }}>
            {totalCount === 0
              ? "Sin resultados"
              : `${totalCount} misión${totalCount !== 1 ? "es" : ""} encontrada${totalCount !== 1 ? "s" : ""}`}
          </div>

          <table style={tableStyle}>
            <thead>
              <tr>
                <th style={thStyle}>Título</th>
                <th style={thStyle}>Dificultad</th>
                <th style={thStyle}>Tipo</th>
                <th style={thStyle}>Estado</th>
              </tr>
            </thead>
            <tbody>
              {items.length === 0 ? (
                <tr>
                  <td colSpan={4} style={{ ...tdStyle, textAlign: "center", color: "#666" }}>
                    No se encontraron misiones
                  </td>
                </tr>
              ) : (
                items.map((item) => (
                  <tr key={item.id}>
                    <td style={tdStyle}>{item.title}</td>
                    <td style={tdStyle}>
                      <span style={badgeStyle(difficultyBadgeColor(item.difficulty))}>
                        {item.difficulty}
                      </span>
                    </td>
                    <td style={tdStyle}>{item.type}</td>
                    <td style={tdStyle}>
                      <span style={badgeStyle(statusBadgeColor(item.status))}>
                        {item.status === "Active" ? "Activa" : item.status === "Draft" ? "Borrador" : item.status}
                      </span>
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