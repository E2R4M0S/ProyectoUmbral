import { useState, useEffect, type FormEvent } from "react";
import { listSessions, transitionSession, ApiError } from "../../services/sessionsApi";
import { Link, useLocation } from "react-router-dom";
import type { SessionListItem, GetSessionsParams, SessionStatus } from "../../types/session";

const STATUS_OPTIONS: {value:string;label:string}[] = [
  { value: "", label: "Todos los estados" },
  { value: "Scheduled", label: "Programada" },
  { value: "Preparing", label: "En Preparacion" },
  { value: "Active", label: "Activa" },
  { value: "Paused", label: "Pausada" },
  { value: "Finished", label: "Finalizada" },
  { value: "Cancelled", label: "Cancelada" },
];

function ActionButton({ item, onReload }: { item: SessionListItem; onReload: () => void }) {
  const transitions = getTransitions(item.status as SessionStatus);
  if (transitions.length === 0) return null;
  return <>
    {transitions.map(t => (
      <button key={t} onClick={async () => { try { await transitionSession(item.id, t); onReload(); } catch {} }}
        style={{ padding: "4px 8px", border: "none", borderRadius: 4, cursor: "pointer", fontSize: 11, fontWeight: 600, color: "white", marginRight: 4,
          backgroundColor: t === "Cancelled" || t === "Finished" ? "#dc3545" : t === "Active" ? "#28a745" : "#0f3460" }}>
        {t === "Preparing" ? "Preparar" : t === "Active" ? "Iniciar" : t === "Paused" ? "Pausar" : t === "Finished" ? "Finalizar" : t}
      </button>
    ))}
  </>;
}

function getTransitions(status: SessionStatus): string[] {
  switch (status) { case "Scheduled": return ["Preparing", "Cancelled"]; case "Preparing": return ["Active", "Cancelled"]; case "Active": return ["Paused", "Finished", "Cancelled"]; case "Paused": return ["Active", "Finished", "Cancelled"]; default: return []; }
}

export function ListadoSesiones() {
  const location = useLocation();
  const basePath = location.pathname.startsWith("/admin") ? "/admin" : "/operator";
  const [items, setItems] = useState<SessionListItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const pageSize = 10;
  const totalPages = Math.ceil(totalCount / pageSize);

  async function loadSessions() {
    setLoading(true); setError(null);
    try {
      const p: GetSessionsParams = { page, pageSize };
      if (search.trim()) p.search = search.trim();
      if (status) p.status = status;
      const r = await listSessions(p);
      setItems(r.items); setTotalCount(r.totalCount);
    } catch (e) { setError("Error al cargar"); }
    finally { setLoading(false); }
  }
  useEffect(() => { loadSessions(); }, [page]);

  const cs: Record<string, React.CSSProperties> = {
    table: { width: "100%", borderCollapse: "collapse", fontSize: 14 },
    th: { textAlign: "left", padding: "10px 12px", borderBottom: "2px solid #e94560", backgroundColor: "#16213e", fontWeight: 600, color: "white" },
    td: { padding: "10px 12px", borderBottom: "1px solid #0f3460", verticalAlign: "middle", color: "white" },
    inp: { width: "100%", padding: 8, border: "1px solid #0f3460", borderRadius: 4, boxSizing: "border-box", backgroundColor: "#16213e", color: "white" },
    badge: (bg: string) => ({ display: "inline-block", padding: "2px 8px", borderRadius: 12, fontSize: 12, fontWeight: 600, color: "white", backgroundColor: bg }),
  };

  const statusColors: Record<string, string> = { Scheduled: "#6c757d", Preparing: "#ffc107", Active: "#28a745", Paused: "#ffc107", Finished: "#007bff", Cancelled: "#dc3545" };
  const statusLabels: Record<string, string> = { Scheduled: "Programada", Preparing: "En Preparacion", Active: "Activa", Paused: "Pausada", Finished: "Finalizada", Cancelled: "Cancelada" };

  return (
    <div style={{ padding: "1rem", maxWidth: 1200, margin: "0 auto" }}>
      <h2 style={{ marginBottom: "1rem", color: "white" }}>Sesiones</h2>
      {error && <div style={{ color: "#e94560", marginBottom: 12 }}>{error}</div>}
      <div style={{ display: "flex", gap: 12, marginBottom: 16, flexWrap: "wrap" }}>
        <input placeholder="Buscar..." value={search} onChange={e => setSearch(e.target.value)} style={cs.inp} />
        <select value={status} onChange={e => { setStatus(e.target.value); setPage(1); }} style={{ ...cs.inp, cursor: "pointer" }}>
          {STATUS_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
        </select>
        <button onClick={loadSessions} style={{ padding: "8px 16px", backgroundColor: "#0f3460", color: "white", border: "none", borderRadius: 4, cursor: "pointer" }}>Buscar</button>
      </div>
      {loading ? <p style={{ color: "#aaa" }}>Cargando...</p> : (
        <table style={cs.table}>
          <thead><tr><th style={cs.th}>Nombre</th><th style={cs.th}>Mision</th><th style={cs.th}>Estado</th><th style={cs.th}>PIN</th><th style={cs.th}>Fecha</th><th style={cs.th}>Accion</th></tr></thead>
          <tbody>
            {items.length === 0 ? <tr><td colSpan={6} style={{ ...cs.td, textAlign: "center" }}>No hay sesiones</td></tr> :
              items.map(item => (
                <tr key={item.id}>
                  <td style={cs.td}>{item.name}</td>
                  <td style={cs.td}>{item.missionTitle}</td>
                  <td style={cs.td}><span style={cs.badge(statusColors[item.status] || "#6c757d")}>{statusLabels[item.status] || item.status}</span></td>
                  <td style={cs.td}><code style={{ backgroundColor: "#0f3460", padding: "2px 6px", borderRadius: 4 }}>{item.pin}</code></td>
                  <td style={cs.td}>{new Date(item.createdAt).toLocaleDateString("es-AR")}</td>
                  <td style={cs.td}>
                    <Link to={`${basePath}/sesiones/${item.id}/panel`} style={{ padding: "4px 8px", border: "none", borderRadius: 4, cursor: "pointer", fontSize: 11, fontWeight: 600, color: "white", backgroundColor: "#28a745", marginRight: 4, textDecoration: "none" }}>
                      Panel
                    </Link>
                    <ActionButton item={item} onReload={loadSessions} />
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      )}
      {totalPages > 1 && <div style={{ display: "flex", gap: 8, justifyContent: "center", marginTop: 16 }}>
        <button onClick={() => setPage(p => p - 1)} disabled={page <= 1} style={{ padding: "6px 12px", backgroundColor: "#0f3460", color: "white", border: "none", borderRadius: 4, cursor: "pointer" }}>Anterior</button>
        <span style={{ color: "#ccc" }}>Pagina {page} de {totalPages}</span>
        <button onClick={() => setPage(p => p + 1)} disabled={page >= totalPages} style={{ padding: "6px 12px", backgroundColor: "#0f3460", color: "white", border: "none", borderRadius: 4, cursor: "pointer" }}>Siguiente</button>
      </div>}
    </div>
  );
}
