import { useState, useEffect } from "react";
import { useParams, Link } from "react-router-dom";
import { getMissionById, createStage, updateStage, deleteStage, createClue, deleteClue, ApiError } from "../../services/missionsApi";
import { fetchWithAuth } from "../../services/api";
import type { MissionDetail } from "../../types/mission";

// ── Styles ────────────────────────────────────────────────────────────────────

const css = {
  container: { maxWidth: 820, margin: "0 auto", color: "white", padding: "0 0 2rem" } as React.CSSProperties,
  backLink: { color: "#e94560", textDecoration: "none", fontSize: "0.875rem", display: "inline-flex", alignItems: "center", gap: 4, marginBottom: "1.25rem" } as React.CSSProperties,

  // Mission header
  missionCard: { backgroundColor: "#16213e", border: "1px solid #0f3460", borderRadius: 10, padding: "1.25rem 1.5rem", marginBottom: "1.75rem" } as React.CSSProperties,
  missionTitle: { fontSize: "1.4rem", fontWeight: 700, color: "#e94560", margin: "0 0 0.25rem" } as React.CSSProperties,
  missionDesc: { color: "#ccc", fontSize: "0.9rem", margin: "0 0 0.75rem", lineHeight: 1.5 } as React.CSSProperties,
  metaRow: { display: "flex", flexWrap: "wrap" as const, gap: "0.5rem", alignItems: "center" } as React.CSSProperties,
  metaChip: (bg: string): React.CSSProperties => ({ padding: "3px 10px", borderRadius: 20, fontSize: 12, fontWeight: 600, backgroundColor: bg, color: "white" }),

  // Section
  sectionHeader: { display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "0.75rem" } as React.CSSProperties,
  sectionTitle: { fontSize: "1rem", fontWeight: 700, color: "#e94560", margin: 0, textTransform: "uppercase" as const, letterSpacing: 1 } as React.CSSProperties,

  // Stage card
  stageCard: { backgroundColor: "#16213e", border: "1px solid #0f3460", borderLeft: "4px solid #e94560", borderRadius: 8, marginBottom: "0.75rem", overflow: "hidden" } as React.CSSProperties,
  stageBody: { padding: "0.875rem 1rem" } as React.CSSProperties,
  stageTop: { display: "flex", justifyContent: "space-between", alignItems: "flex-start", gap: "1rem" } as React.CSSProperties,
  stageInfo: { flex: 1, minWidth: 0 } as React.CSSProperties,
  stageOrderBadge: { display: "inline-flex", alignItems: "center", justifyContent: "center", width: 24, height: 24, borderRadius: "50%", backgroundColor: "#e94560", color: "white", fontSize: 12, fontWeight: 700, flexShrink: 0, marginRight: 8 } as React.CSSProperties,
  stageName: { fontWeight: 600, fontSize: "0.95rem", display: "flex", alignItems: "center" } as React.CSSProperties,
  stageDesc: { color: "#aaa", fontSize: "0.82rem", marginTop: 4, marginLeft: 32, lineHeight: 1.4 } as React.CSSProperties,
  stageActions: { display: "flex", gap: "0.375rem", flexShrink: 0, flexWrap: "wrap" as const } as React.CSSProperties,

  // Buttons
  btnPrimary: { padding: "5px 12px", backgroundColor: "#0f3460", color: "white", border: "none", borderRadius: 5, cursor: "pointer", fontSize: "0.78rem", fontWeight: 600, whiteSpace: "nowrap" as const } as React.CSSProperties,
  btnDanger: { padding: "5px 12px", backgroundColor: "transparent", color: "#e94560", border: "1px solid #e94560", borderRadius: 5, cursor: "pointer", fontSize: "0.78rem", fontWeight: 600, whiteSpace: "nowrap" as const } as React.CSSProperties,
  btnSuccess: { padding: "5px 12px", backgroundColor: "#1a4d2e", color: "#4caf50", border: "1px solid #4caf50", borderRadius: 5, cursor: "pointer", fontSize: "0.78rem", fontWeight: 600, whiteSpace: "nowrap" as const } as React.CSSProperties,
  btnSecondary: { padding: "5px 12px", backgroundColor: "transparent", color: "#888", border: "1px solid #444", borderRadius: 5, cursor: "pointer", fontSize: "0.78rem", fontWeight: 600 } as React.CSSProperties,
  btnAddStage: { padding: "10px 20px", backgroundColor: "#e94560", color: "white", border: "none", borderRadius: 7, cursor: "pointer", fontSize: "0.875rem", fontWeight: 600, display: "flex", alignItems: "center", gap: 6 } as React.CSSProperties,

  // Inline edit/create form
  inlineForm: { borderTop: "1px solid #0f3460", backgroundColor: "#0d1b35", padding: "1rem" } as React.CSSProperties,
  formRow: { display: "grid", gridTemplateColumns: "1fr 1fr", gap: "0.75rem", marginBottom: "0.75rem" } as React.CSSProperties,
  formRowFull: { marginBottom: "0.75rem" } as React.CSSProperties,
  label: { display: "block", fontSize: "0.78rem", fontWeight: 600, color: "#aaa", marginBottom: 4, textTransform: "uppercase" as const, letterSpacing: 0.5 } as React.CSSProperties,
  input: { width: "100%", padding: "8px 10px", border: "1px solid #0f3460", borderRadius: 5, backgroundColor: "#16213e", color: "white", fontSize: "0.875rem", boxSizing: "border-box" as const, outline: "none" } as React.CSSProperties,
  formActions: { display: "flex", gap: "0.5rem", justifyContent: "flex-end" } as React.CSSProperties,

  // Clues
  clueList: { borderTop: "1px solid #0f3460", padding: "0.5rem 1rem" } as React.CSSProperties,
  clueItem: { display: "flex", justifyContent: "space-between", alignItems: "center", padding: "0.4rem 0.75rem", backgroundColor: "#0f2040", borderRadius: 5, marginBottom: "0.35rem" } as React.CSSProperties,
  clueText: { fontSize: "0.82rem", color: "#ccc", flex: 1 } as React.CSSProperties,
  clueDeleteBtn: { background: "none", border: "none", color: "#e94560", cursor: "pointer", fontSize: 14, padding: "0 4px", lineHeight: 1, marginLeft: 8 } as React.CSSProperties,
  addClueRow: { display: "flex", gap: "0.5rem", marginTop: "0.5rem", paddingBottom: "0.5rem" } as React.CSSProperties,
  addClueInput: { flex: 1, padding: "6px 10px", border: "1px solid #0f3460", borderRadius: 5, backgroundColor: "#16213e", color: "white", fontSize: "0.82rem", outline: "none" } as React.CSSProperties,
  addClueBtn: { padding: "6px 14px", backgroundColor: "#4caf50", color: "white", border: "none", borderRadius: 5, cursor: "pointer", fontSize: "0.82rem", fontWeight: 600, whiteSpace: "nowrap" as const } as React.CSSProperties,

  // Notifications
  msgSuccess: { padding: "10px 14px", backgroundColor: "#1a3d1a", border: "1px solid #4caf50", borderRadius: 6, color: "#4caf50", fontSize: "0.85rem", marginBottom: "1rem" } as React.CSSProperties,
  msgError: { padding: "10px 14px", backgroundColor: "#2d1a1a", border: "1px solid #e94560", borderRadius: 6, color: "#e94560", fontSize: "0.85rem", marginBottom: "1rem" } as React.CSSProperties,
};

// ── Component ─────────────────────────────────────────────────────────────────

export function DetalleMision() {
  const { id } = useParams<{ id: string }>();
  const [mission, setMission] = useState<MissionDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [showStageForm, setShowStageForm] = useState(false);
  const [stageForm, setStageForm] = useState({ name: "", description: "", latitude: "", longitude: "" });
  const [editingStage, setEditingStage] = useState<string | null>(null);
  const [editForm, setEditForm] = useState({ name: "", description: "", order: 1, latitude: "", longitude: "" });
  const [clueForm, setClueForm] = useState<Record<string, string>>({});
  const [showClueForm, setShowClueForm] = useState<string | null>(null);
  const [msg, setMsg] = useState("");
  const [msgType, setMsgType] = useState<"success" | "error">("success");

  function notify(text: string, type: "success" | "error" = "success") {
    setMsg(text);
    setMsgType(type);
    if (type === "success") setTimeout(() => setMsg(""), 3000);
  }

  async function load() {
    if (!id) return;
    setLoading(true);
    try { setMission(await getMissionById(id)); setError(""); }
    catch { setError("No se pudo cargar la misión."); }
    finally { setLoading(false); }
  }
  useEffect(() => { load(); }, [id]);

  function nextOrder(stages: MissionDetail["stages"]) {
    if (!stages.length) return 1;
    return Math.max(...stages.map(s => s.order)) + 1;
  }

  async function handleCreateStage() {
    if (!id || !stageForm.name.trim() || !mission) return;
    const lat = stageForm.latitude ? parseFloat(stageForm.latitude) : undefined;
    const lng = stageForm.longitude ? parseFloat(stageForm.longitude) : undefined;
    try {
      await createStage(id, { name: stageForm.name, description: stageForm.description, order: nextOrder(mission.stages), latitude: lat, longitude: lng });
      setShowStageForm(false);
      setStageForm({ name: "", description: "", latitude: "", longitude: "" });
      notify("Etapa creada correctamente");
      load();
    } catch (e) { notify(e instanceof ApiError ? e.body : "Error al crear etapa", "error"); }
  }

  async function handleUpdateStage(stageId: string) {
    if (!id) return;
    const lat = editForm.latitude ? parseFloat(editForm.latitude) : undefined;
    const lng = editForm.longitude ? parseFloat(editForm.longitude) : undefined;
    try {
      await updateStage(id, stageId, { name: editForm.name, description: editForm.description, order: editForm.order, latitude: lat, longitude: lng });
      setEditingStage(null);
      notify("Etapa actualizada correctamente");
      load();
    } catch { notify("Error al actualizar la etapa", "error"); }
  }

  async function handleDeleteStage(stageId: string) {
    if (!id || !confirm("¿Eliminar esta etapa y todas sus pistas?")) return;
    try { await deleteStage(id, stageId); notify("Etapa eliminada"); load(); }
    catch (e) { notify(e instanceof ApiError ? e.body : "Error al eliminar etapa", "error"); }
  }

  async function handleAddClue(stageId: string) {
    if (!id || !clueForm[stageId]?.trim()) return;
    try {
      await createClue(id, stageId, { content: clueForm[stageId] });
      setClueForm(f => ({ ...f, [stageId]: "" }));
      setShowClueForm(null);
      load();
    } catch { notify("Error al agregar pista", "error"); }
  }

  async function handleDeleteClue(stageId: string, clueId: string) {
    if (!id) return;
    try { await deleteClue(id, stageId, clueId); load(); }
    catch { notify("Error al eliminar pista", "error"); }
  }

  async function handleDownloadQr(stageId: string, stageName: string) {
    if (!id) return;
    try {
      const response = await fetchWithAuth(`/api/missions/${id}/stages/${stageId}/qr`);
      if (!response.ok) { notify("Error al generar el QR", "error"); return; }
      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `qr-${stageName.replace(/\s+/g, "-").toLowerCase()}.png`;
      a.click();
      URL.revokeObjectURL(url);
    } catch { notify("Error al descargar el QR", "error"); }
  }

  if (loading) return <div style={css.container}><p style={{ color: "#aaa" }}>Cargando...</p></div>;
  if (error || !mission) return (
    <div style={css.container}>
      <p style={css.msgError}>{error || "Misión no encontrada"}</p>
      <Link to="/admin/misiones" style={css.backLink}>← Volver</Link>
    </div>
  );

  const statusColors: Record<string, string> = { Active: "#007bff", Draft: "#6c757d", Inactive: "#dc3545" };
  const difficultyColors: Record<string, string> = { Easy: "#28a745", Medium: "#fd7e14", Hard: "#dc3545" };

  return (
    <div style={css.container}>
      <Link to="/admin/misiones" style={css.backLink}>← Volver al catálogo</Link>

      {/* Mission header card */}
      <div style={css.missionCard}>
        <h2 style={css.missionTitle}>{mission.title}</h2>
        <p style={css.missionDesc}>{mission.description}</p>
        <div style={css.metaRow}>
          <span style={css.metaChip(difficultyColors[mission.difficulty] || "#6c757d")}>{mission.difficulty}</span>
          <span style={css.metaChip("#0f3460")}>{mission.timeMinutes} min</span>
          <span style={css.metaChip("#1a3a4a")}>{mission.type}</span>
          <span style={css.metaChip(statusColors[mission.status] || "#6c757d")}>{mission.status}</span>
        </div>
      </div>

      {msg && <div style={msgType === "error" ? css.msgError : css.msgSuccess}>{msg}</div>}

      {mission.type === "Treasure" && (
        <>
          <div style={css.sectionHeader}>
            <h3 style={css.sectionTitle}>Etapas &nbsp;<span style={{ color: "#666", fontWeight: 400 }}>({mission.stages.length})</span></h3>
            {!showStageForm && (
              <button style={css.btnAddStage} onClick={() => { setShowStageForm(true); setStageForm({ name: "", description: "", latitude: "", longitude: "" }); }}>
                + Nueva Etapa
              </button>
            )}
          </div>

          {/* New stage form */}
          {showStageForm && (
            <div style={{ ...css.stageCard, marginBottom: "1rem" }}>
              <div style={{ ...css.stageBody, borderLeft: "4px solid #4caf50" }}>
                <p style={{ margin: "0 0 0.75rem", fontWeight: 600, color: "#4caf50", fontSize: "0.875rem" }}>
                  Nueva etapa — Orden {nextOrder(mission.stages)} (asignado automáticamente)
                </p>
              </div>
              <div style={css.inlineForm}>
                <div style={css.formRow}>
                  <div>
                    <label style={css.label}>Nombre *</label>
                    <input
                      style={css.input}
                      placeholder="Ej: Biblioteca Central"
                      value={stageForm.name}
                      autoFocus
                      onChange={e => setStageForm(f => ({ ...f, name: e.target.value }))}
                    />
                  </div>
                  <div>
                    <label style={css.label}>Descripción</label>
                    <input
                      style={css.input}
                      placeholder="Descripción de la ubicación"
                      value={stageForm.description}
                      onChange={e => setStageForm(f => ({ ...f, description: e.target.value }))}
                    />
                  </div>
                </div>
                <div style={{ ...css.formRow, marginBottom: "0.75rem" }}>
                  <div>
                    <label style={css.label}>Latitud (opcional)</label>
                    <input
                      style={css.input}
                      type="number"
                      step="any"
                      placeholder="Ej: 10.492"
                      value={stageForm.latitude}
                      onChange={e => setStageForm(f => ({ ...f, latitude: e.target.value }))}
                    />
                  </div>
                  <div>
                    <label style={css.label}>Longitud (opcional)</label>
                    <input
                      style={css.input}
                      type="number"
                      step="any"
                      placeholder="Ej: -66.902"
                      value={stageForm.longitude}
                      onChange={e => setStageForm(f => ({ ...f, longitude: e.target.value }))}
                    />
                  </div>
                </div>
                <div style={css.formActions}>
                  <button style={css.btnSecondary} onClick={() => setShowStageForm(false)}>Cancelar</button>
                  <button style={{ ...css.btnAddStage, padding: "8px 18px", fontSize: "0.82rem" }} onClick={handleCreateStage}>
                    Crear Etapa
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* Stage list */}
          {mission.stages.length === 0 && !showStageForm && (
            <p style={{ color: "#555", fontSize: "0.875rem", padding: "1rem 0" }}>
              No hay etapas. Agregá la primera para estructurar la búsqueda.
            </p>
          )}

          {[...mission.stages].sort((a, b) => a.order - b.order).map(stage => (
            <div key={stage.id} style={css.stageCard}>
              <div style={css.stageBody}>
                <div style={css.stageTop}>
                  <div style={css.stageInfo}>
                    <div style={css.stageName}>
                      <span style={css.stageOrderBadge}>{stage.order}</span>
                      {stage.name}
                    </div>
                    {stage.description && <p style={css.stageDesc}>{stage.description}</p>}
                    {stage.latitude != null && stage.longitude != null && (
                      <p style={{ ...css.stageDesc, color: "#4caf50", fontSize: "0.78rem" }}>
                        🌍 {stage.latitude}, {stage.longitude}
                      </p>
                    )}
                  </div>
                  <div style={css.stageActions}>
                    <button style={css.btnSuccess} onClick={() => handleDownloadQr(stage.id, stage.name)}>⬇ QR</button>
                    <button style={css.btnPrimary} onClick={() => {
                      setEditingStage(stage.id);
                      setEditForm({ name: stage.name, description: stage.description, order: stage.order, latitude: stage.latitude?.toString() ?? "", longitude: stage.longitude?.toString() ?? "" });
                    }}>Editar</button>
                    <button style={css.btnPrimary} onClick={() => setShowClueForm(showClueForm === stage.id ? null : stage.id)}>
                      {showClueForm === stage.id ? "Cerrar" : "+ Pista"}
                    </button>
                    <button style={css.btnDanger} onClick={() => handleDeleteStage(stage.id)}>Eliminar</button>
                  </div>
                </div>
              </div>

              {/* Inline edit form */}
              {editingStage === stage.id && (
                <div style={css.inlineForm}>
                  <div style={css.formRow}>
                    <div>
                      <label style={css.label}>Nombre *</label>
                      <input style={css.input} value={editForm.name} onChange={e => setEditForm(f => ({ ...f, name: e.target.value }))} />
                    </div>
                    <div>
                      <label style={css.label}>Descripción</label>
                      <input style={css.input} value={editForm.description} onChange={e => setEditForm(f => ({ ...f, description: e.target.value }))} />
                    </div>
                  </div>
                  <div style={{ ...css.formRow, marginBottom: "0.75rem" }}>
                    <div>
                      <label style={css.label}>Latitud (opcional)</label>
                      <input style={css.input} type="number" step="any" value={editForm.latitude} onChange={e => setEditForm(f => ({ ...f, latitude: e.target.value }))} />
                    </div>
                    <div>
                      <label style={css.label}>Longitud (opcional)</label>
                      <input style={css.input} type="number" step="any" value={editForm.longitude} onChange={e => setEditForm(f => ({ ...f, longitude: e.target.value }))} />
                    </div>
                  </div>
                  <div style={{ marginBottom: "0.75rem", maxWidth: 120 }}>
                    <label style={css.label}>Orden</label>
                    <input style={css.input} type="number" min="1" value={editForm.order} onChange={e => setEditForm(f => ({ ...f, order: Number(e.target.value) }))} />
                  </div>
                  <div style={css.formActions}>
                    <button style={css.btnSecondary} onClick={() => setEditingStage(null)}>Cancelar</button>
                    <button style={{ ...css.btnAddStage, padding: "8px 18px", fontSize: "0.82rem" }} onClick={() => handleUpdateStage(stage.id)}>Guardar</button>
                  </div>
                </div>
              )}

              {/* Clues */}
              {(stage.clues.length > 0 || showClueForm === stage.id) && (
                <div style={css.clueList}>
                  {stage.clues.map(clue => (
                    <div key={clue.id} style={css.clueItem}>
                      <span style={css.clueText}>💡 {clue.content}{clue.penalty != null ? <span style={{ color: "#e94560", marginLeft: 8 }}>−{clue.penalty} pts</span> : null}</span>
                      <button style={css.clueDeleteBtn} onClick={() => handleDeleteClue(stage.id, clue.id)}>✕</button>
                    </div>
                  ))}
                  {showClueForm === stage.id && (
                    <div style={css.addClueRow}>
                      <input
                        style={css.addClueInput}
                        placeholder="Escribí el contenido de la pista..."
                        value={clueForm[stage.id] || ""}
                        autoFocus
                        onChange={e => setClueForm(f => ({ ...f, [stage.id]: e.target.value }))}
                        onKeyDown={e => { if (e.key === "Enter") handleAddClue(stage.id); }}
                      />
                      <button style={css.addClueBtn} onClick={() => handleAddClue(stage.id)}>Agregar</button>
                    </div>
                  )}
                </div>
              )}
            </div>
          ))}
        </>
      )}
    </div>
  );
}
