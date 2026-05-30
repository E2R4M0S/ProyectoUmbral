import { useState, useEffect } from "react";
import { useParams, Link } from "react-router-dom";
import { getMissionById, createStage, updateStage, deleteStage, createClue, deleteClue, ApiError } from "../../services/missionsApi";
import type { MissionDetail } from "../../types/mission";

const s: Record<string, React.CSSProperties> = {
  container: { maxWidth: 800, margin: "0 auto", color: "white", fontFamily: "sans-serif" },
  header: { marginBottom: "1.5rem" },
  title: { fontSize: "1.5rem", margin: 0, color: "#e94560" },
  meta: { color: "#999", fontSize: "0.9rem", marginTop: "0.25rem" },
  sectionTitle: { fontSize: "1.1rem", color: "#e94560", margin: "1.5rem 0 0.5rem" },
  stageCard: { padding: "1rem", backgroundColor: "#16213e", borderRadius: 8, border: "1px solid #0f3460", marginBottom: "0.75rem" },
  stageHeader: { display: "flex", justifyContent: "space-between", alignItems: "center" },
  stageName: { fontWeight: 600, fontSize: "1rem" },
  stageDesc: { color: "#aaa", fontSize: "0.85rem", marginTop: "0.25rem" },
  clueItem: { padding: "0.5rem 0.75rem", backgroundColor: "#0f3460", borderRadius: 4, marginTop: "0.5rem", display: "flex", justifyContent: "space-between", alignItems: "center" },
  clueText: { fontSize: "0.85rem" },
  badge: (bg: string) => ({ display: "inline-block", padding: "2px 8px", borderRadius: 12, fontSize: 12, fontWeight: 600, color: "white", backgroundColor: bg }),
  btn: (bg: string) => ({ padding: "6px 12px", backgroundColor: bg, color: "white", border: "none", borderRadius: 4, cursor: "pointer", fontSize: "0.8rem", fontWeight: 600, marginLeft: "0.5rem" }),
  btnSmall: (bg: string) => ({ padding: "4px 8px", backgroundColor: bg, color: "white", border: "none", borderRadius: 4, cursor: "pointer", fontSize: "0.75rem" }),
  input: { width: "100%", padding: 8, border: "1px solid #0f3460", borderRadius: 4, backgroundColor: "#16213e", color: "white", marginBottom: 8, boxSizing: "border-box" as const },
  backLink: { color: "#e94560", textDecoration: "none", fontSize: "0.9rem" },
  error: { color: "#e94560", marginBottom: 12, padding: "8px 12px", backgroundColor: "#2d1a1a", border: "1px solid #e94560", borderRadius: 4, fontSize: "0.85rem" } as React.CSSProperties,
  success: { color: "#28a745", marginBottom: 8, fontSize: "0.85rem" },
};

export function DetalleMision() {
  const { id } = useParams<{ id: string }>();
  const [mission, setMission] = useState<MissionDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [showStageForm, setShowStageForm] = useState(false);
  const [stageForm, setStageForm] = useState({ name: "", description: "", order: (mission?.stages.length ?? 0) + 1 });
  const [editingStage, setEditingStage] = useState<string | null>(null);
  const [clueForm, setClueForm] = useState<Record<string, string>>({});
  const [showClueForm, setShowClueForm] = useState<string | null>(null);
  const [msg, setMsg] = useState("");

  async function load() {
    if (!id) return;
    setLoading(true);
    try { setMission(await getMissionById(id)); setError(""); } catch { setError("No se pudo cargar la misión."); }
    finally { setLoading(false); }
  }
  useEffect(() => { load(); }, [id]);

  async function handleCreateStage() {
    if (!id || !stageForm.name.trim()) return;
    try { await createStage(id, { ...stageForm, order: stageForm.order }); setShowStageForm(false); setStageForm({ name: "", description: "", order: (mission?.stages.length ?? 0) + 1 }); load(); } catch (e) { setMsg(e instanceof ApiError ? e.body : "Error al crear etapa"); }
  }
  async function handleUpdateStage(stageId: string) {
    if (!id) return;
    const s = stageForm;
    try { await updateStage(id, stageId, { name: s.name, description: s.description, order: s.order }); setEditingStage(null); setMsg("Etapa actualizada"); load(); } catch { setMsg("Error al actualizar"); }
  }
  async function handleDeleteStage(stageId: string) {
    if (!id || !confirm("¿Eliminar esta etapa y todas sus pistas?")) return;
    try { await deleteStage(id, stageId); load(); } catch (e) { setMsg(e instanceof ApiError ? e.body : "Error al eliminar etapa"); }
  }
  async function handleAddClue(stageId: string) {
    if (!id || !clueForm[stageId]?.trim()) return;
    try { await createClue(id, stageId, { content: clueForm[stageId] }); setClueForm(f => ({ ...f, [stageId]: "" })); setShowClueForm(null); load(); } catch { setMsg("Error al agregar pista"); }
  }
  async function handleDeleteClue(stageId: string, clueId: string) {
    if (!id) return;
    try { await deleteClue(id, stageId, clueId); load(); } catch { setMsg("Error al eliminar"); }
  }

  if (loading) return <div style={s.container}><p>Cargando...</p></div>;
  if (error || !mission) return <div style={s.container}><p style={s.error}>{error || "Misión no encontrada"}</p><Link to="/admin/misiones" style={s.backLink}>← Volver</Link></div>;

  const statusColors: Record<string, string> = { Active: "#007bff", Draft: "#6c757d", Inactive: "#dc3545" };

  return (
    <div style={s.container}>
      <Link to="/admin/misiones" style={s.backLink}>← Volver al catálogo</Link>
      <div style={s.header}>
        <h2 style={s.title}>{mission.title}</h2>
        <p style={{ ...s.meta, color: "#ccc" }}>{mission.description}</p>
        <p style={s.meta}>
          {mission.difficulty} · {mission.timeMinutes}min · {mission.type} · <span style={s.badge(statusColors[mission.status] || "#6c757d")}>{mission.status}</span>
        </p>
      </div>

      {msg && <p style={msg.includes("Error") ? s.error : s.success}>{msg}</p>}

      {mission.type === "Treasure" ? (
        <>
      <h3 style={s.sectionTitle}>Etapas ({mission.stages.length})</h3>
      {mission.stages.map(stage => (
        <div key={stage.id} style={s.stageCard}>
          <div style={s.stageHeader}>
            <div>
              <span style={s.stageName}>#{stage.order} {stage.name}</span>
              <p style={s.stageDesc}>{stage.description}</p>
            </div>
            <div>
              <button style={s.btn("#0f3460")} onClick={() => { setEditingStage(stage.id); setStageForm({ name: stage.name, description: stage.description, order: stage.order }); }}>Editar</button>
              <button style={s.btn("#e94560")} onClick={() => handleDeleteStage(stage.id)}>Eliminar</button>
              <button style={s.btn("#e94560")} onClick={() => { setShowClueForm(showClueForm === stage.id ? null : stage.id); }}>+ Pista</button>
            </div>
          </div>
          {editingStage === stage.id && (
            <div style={{ marginTop: "0.5rem", padding: "0.75rem", backgroundColor: "#0f3460", borderRadius: 4 }}>
              <input style={s.input} placeholder="Nombre" value={stageForm.name} onChange={e => setStageForm(f => ({ ...f, name: e.target.value }))} />
              <input style={s.input} placeholder="Descripción" value={stageForm.description} onChange={e => setStageForm(f => ({ ...f, description: e.target.value }))} />
              <input style={s.input} placeholder="Orden" type="number" min="1" value={stageForm.order} onChange={e => setStageForm(f => ({ ...f, order: Number(e.target.value) }))} />
              <button style={s.btn("#28a745")} onClick={() => handleUpdateStage(stage.id)}>Guardar</button>
              <button style={s.btn("#6c757d")} onClick={() => setEditingStage(null)}>Cancelar</button>
            </div>
          )}
          {stage.clues.map(clue => (
            <div key={clue.id} style={s.clueItem}>
              <span style={s.clueText}>💡 {clue.content} {clue.penalty != null ? `(-${clue.penalty}pts)` : ""}</span>
              <button style={s.btnSmall("#dc3545")} onClick={() => handleDeleteClue(stage.id, clue.id)}>✕</button>
            </div>
          ))}
          {showClueForm === stage.id && (
            <div style={{ marginTop: "0.5rem", display: "flex", gap: "0.5rem" }}>
              <input style={{ ...s.input, marginBottom: 0 }} placeholder="Contenido de la pista" value={clueForm[stage.id] || ""} onChange={e => setClueForm(f => ({ ...f, [stage.id]: e.target.value }))} />
              <button style={s.btn("#28a745")} onClick={() => handleAddClue(stage.id)}>✓</button>
            </div>
          )}
        </div>
      ))}

      {showStageForm ? (
        <div style={{ ...s.stageCard, marginTop: "1rem" }}>
          <input style={s.input} placeholder="Nombre de la etapa" value={stageForm.name} onChange={e => setStageForm(f => ({ ...f, name: e.target.value }))} />
          <input style={s.input} placeholder="Descripción" value={stageForm.description} onChange={e => setStageForm(f => ({ ...f, description: e.target.value }))} />
          <input style={s.input} placeholder="Orden" type="number" value={stageForm.order} onChange={e => setStageForm(f => ({ ...f, order: Number(e.target.value) }))} />
          <button style={s.btn("#28a745")} onClick={handleCreateStage}>Crear Etapa</button>
          <button style={s.btn("#6c757d")} onClick={() => setShowStageForm(false)}>Cancelar</button>
        </div>
      ) : (
        <button style={{ ...s.btn("#e94560"), marginTop: "1rem", marginLeft: 0 }} onClick={() => setShowStageForm(true)}>+ Agregar Etapa</button>
      )}
        </>
      ) : null}
    </div>
  );
}
