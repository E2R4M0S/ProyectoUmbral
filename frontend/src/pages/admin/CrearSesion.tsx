import { useState, useEffect, useRef, type FormEvent } from "react";
import { createSession, ApiError } from "../../services/sessionsApi";
import { listMissions, getMissionById } from "../../services/missionsApi";
import { fetchWithAuth } from "../../services/api";
import type { MissionListItem } from "../../types/mission";
import type { StageInput } from "../../types/session";

interface QuizOption {
  id: string;
  title: string;
  questionCount: number;
}

interface MissionEntry {
  missionId: string;
  missionTitle: string;
  missionType: string;
  timeMinutes: number;
  stageCount: number;
  stages: StageInput[];
  quizId?: string;
  quizTitle?: string;
  questionCount?: number;
}

interface FieldErrors {
  name?: string;
  stages?: string;
}

function validateName(value: string): string | undefined {
  if (!value.trim()) return "El nombre es obligatorio.";
  if (value.trim().length > 100) return "El nombre no puede exceder los 100 caracteres.";
  return undefined;
}

const inputStyle = (hasError: boolean): React.CSSProperties => ({
  width: "100%",
  padding: 8,
  border: hasError ? "1px solid #dc3545" : "1px solid #0f3460",
  borderRadius: 4,
  boxSizing: "border-box",
  backgroundColor: "#16213e",
  color: "white",
});

const labelStyle: React.CSSProperties = {
  display: "block",
  marginBottom: 4,
  fontWeight: 600,
};

const errorStyle: React.CSSProperties = {
  color: "#e94560",
  fontSize: 12,
  margin: "4px 0 0",
};

const fieldGroupStyle: React.CSSProperties = {
  marginBottom: 14,
};

const submitBtnStyle = (disabled: boolean): React.CSSProperties => ({
  width: "100%",
  padding: 10,
  backgroundColor: disabled ? "#999" : "#0f3460",
  color: "#fff",
  border: "none",
  borderRadius: 4,
  cursor: disabled ? "not-allowed" : "pointer",
  fontSize: 16,
  fontWeight: 600,
});

const containerStyle: React.CSSProperties = {
  maxWidth: 500,
  margin: "0 auto",
  padding: "1rem",
};

const successStyle: React.CSSProperties = {
  marginBottom: 16,
  padding: "12px 16px",
  border: "1px solid #28a745",
  borderRadius: 4,
  backgroundColor: "#1a4d1a",
  color: "#28a745",
};

const errorMsgStyle: React.CSSProperties = {
  color: "#e94560",
  marginBottom: 16,
  padding: "8px 12px",
  border: "1px solid #e94560",
  borderRadius: 4,
  backgroundColor: "#2d1a1a",
};

const dropdownStyle: React.CSSProperties = {
  position: "absolute",
  top: "100%",
  left: 0,
  right: 0,
  backgroundColor: "#16213e",
  border: "1px solid #0f3460",
  borderRadius: 4,
  maxHeight: 200,
  overflowY: "auto",
  zIndex: 10,
  listStyle: "none",
  margin: 0,
  padding: 0,
};

const dropdownItemStyle: React.CSSProperties = {
  padding: "8px 12px",
  cursor: "pointer",
  fontSize: "0.9rem",
  borderBottom: "1px solid #0f3460",
};

const missionCardStyle: React.CSSProperties = {
  display: "flex",
  alignItems: "flex-start",
  justifyContent: "space-between",
  padding: "10px 12px",
  marginBottom: 6,
  backgroundColor: "#16213e",
  border: "1px solid #0f3460",
  borderRadius: 6,
  gap: 10,
};

const orderBadgeStyle: React.CSSProperties = {
  display: "inline-flex",
  alignItems: "center",
  justifyContent: "center",
  minWidth: 28,
  height: 28,
  padding: "0 8px",
  borderRadius: 14,
  backgroundColor: "#0f3460",
  color: "white",
  fontSize: 12,
  fontWeight: 700,
  flexShrink: 0,
  marginTop: 2,
};

const removeBtnStyle: React.CSSProperties = {
  background: "none",
  border: "1px solid #e94560",
  color: "#e94560",
  borderRadius: 4,
  padding: "4px 8px",
  fontSize: 12,
  cursor: "pointer",
  flexShrink: 0,
};

export function CrearSesion() {
  const [name, setName] = useState("");
  const [missions, setMissions] = useState<MissionEntry[]>([]);
  const [missionSearch, setMissionSearch] = useState("");
  const [missionResults, setMissionResults] = useState<MissionListItem[]>([]);
  const [showDropdown, setShowDropdown] = useState(false);
  const [searching, setSearching] = useState(false);
  const [searchError, setSearchError] = useState(false);
  const [addingMission, setAddingMission] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [success, setSuccess] = useState(false);
  const [createdSession, setCreatedSession] = useState<{ id: string; pin: string } | null>(null);
  const [quizzes, setQuizzes] = useState<QuizOption[]>([]);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const searchIdRef = useRef(0);
  const missionsRef = useRef<MissionEntry[]>([]);

  useEffect(() => { missionsRef.current = missions; }, [missions]);

  useEffect(() => {
    fetchWithAuth("/api/quizzes").then(r => r.json()).then(setQuizzes).catch(() => {});
  }, []);

  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setShowDropdown(false);
      }
    }
    document.addEventListener("mousedown", handleClick);
    return () => document.removeEventListener("mousedown", handleClick);
  }, []);

  function searchMissions(query: string) {
    setMissionSearch(query);
    if (query.trim().length < 2) {
      setMissionResults([]);
      setShowDropdown(false);
      setSearchError(false);
      return;
    }
    setSearching(true);
    setSearchError(false);
    const thisSearchId = ++searchIdRef.current;
    listMissions({ search: query, status: "Active", page: 1, pageSize: 10 })
      .then((result) => {
        if (thisSearchId !== searchIdRef.current) return;
        const selectedIds = new Set(missionsRef.current.map((m) => m.missionId));
        const filtered = result.items.filter((m) => !selectedIds.has(m.id));
        setMissionResults(filtered);
        setShowDropdown(true);
        setSearching(false);
      })
      .catch(() => {
        if (thisSearchId !== searchIdRef.current) return;
        setMissionResults([]);
        setSearchError(true);
        setShowDropdown(true);
        setSearching(false);
      });
  }

  async function addMission(item: MissionListItem) {
    searchIdRef.current++;
    setMissionSearch("");
    setMissionResults([]);
    setShowDropdown(false);
    setAddingMission(true);
    setSubmitError(null);

    try {
      const detail = await getMissionById(item.id);
      const isTreasure = item.type === "Treasure";

      if (isTreasure) {
        const sortedStages = [...detail.stages].sort((a, b) => a.order - b.order);
        if (sortedStages.length === 0) {
          setSubmitError(`La misión "${item.title}" no tiene etapas configuradas.`);
          return;
        }
        setMissions((prev) => {
          let nextOrder = prev.reduce((acc, m) => acc + m.stages.length, 0) + 1;
          const newStages: StageInput[] = sortedStages.map(s => ({
            missionId: item.id,
            missionStageId: s.id,
            missionTitle: item.title,
            stageName: s.name,
            missionType: item.type,
            order: nextOrder++,
            qrToken: s.qrToken,
            timeMinutes: detail.timeMinutes,
          }));
          const entry: MissionEntry = {
            missionId: item.id,
            missionTitle: item.title,
            missionType: item.type,
            timeMinutes: detail.timeMinutes,
            stageCount: sortedStages.length,
            stages: newStages,
          };
          return [...prev, entry];
        });
      } else {
        setMissions((prev) => {
          const nextOrder = prev.reduce((acc, m) => acc + m.stages.length, 0) + 1;
          const stage: StageInput = {
            missionId: item.id,
            missionStageId: "00000000-0000-0000-0000-000000000000",
            missionTitle: item.title,
            stageName: item.title,
            missionType: item.type,
            order: nextOrder,
            qrToken: "00000000-0000-0000-0000-000000000000",
            timeMinutes: 0,
          };
          return [...prev, {
            missionId: item.id,
            missionTitle: item.title,
            missionType: item.type,
            timeMinutes: 0,
            stageCount: 1,
            stages: [stage],
            quizId: "",
            quizTitle: "",
            questionCount: 0,
          }];
        });
      }
    } catch {
      setSubmitError(`No se pudo cargar la misión "${item.title}".`);
    } finally {
      setAddingMission(false);
    }
  }

  function removeMission(missionId: string) {
    setMissions((prev) => {
      const filtered = prev.filter((m) => m.missionId !== missionId);
      // Recompute global stage orders
      let order = 1;
      return filtered.map((m) => ({
        ...m,
        stages: m.stages.map(s => ({ ...s, order: order++ })),
      }));
    });
  }

  function selectQuizForMission(missionId: string, quizId: string) {
    const quiz = quizzes.find(q => q.id === quizId);
    if (!quiz) {
      setMissions(prev => prev.map(m => m.missionId !== missionId ? m : {
        ...m, quizId: "", quizTitle: "", questionCount: 0, timeMinutes: 0,
        stages: m.stages.map(s => ({ ...s, missionStageId: "00000000-0000-0000-0000-000000000000", timeMinutes: 0 })),
      }));
      return;
    }
    const timeMinutes = Math.max(1, Math.ceil(quiz.questionCount * 30 / 60));
    setMissions(prev => prev.map(m => m.missionId !== missionId ? m : {
      ...m, quizId: quiz.id, quizTitle: quiz.title, questionCount: quiz.questionCount, timeMinutes,
      stages: m.stages.map(s => ({ ...s, missionStageId: quiz.id, timeMinutes })),
    }));
  }

  function buildFlatStages(): StageInput[] {
    return missions.flatMap(m => m.stages);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitError(null);
    setSuccess(false);

    const errors: FieldErrors = { name: validateName(name) };
    if (missions.length === 0) errors.stages = "Agregá al menos una misión.";
    else if (missions.some(m => m.missionType === "Trivia" && !m.quizId))
      errors.stages = "Seleccioná un quiz para cada misión de Trivia.";
    setFieldErrors(errors);
    if (Object.values(errors).some(Boolean)) return;

    setIsSubmitting(true);
    try {
      const result = await createSession({
        name: name.trim(),
        stages: buildFlatStages(),
      });
      setSuccess(true);
      setCreatedSession({ id: result.id, pin: result.pin });
      setName("");
      setMissions([]);
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 400) setSubmitError("Datos inválidos. Verifica los campos.");
        else if (err.status === 404) setSubmitError("Misión no encontrada.");
        else setSubmitError("Error al crear la sesión. Inténtalo de nuevo.");
      } else {
        setSubmitError("Error de conexión. Verifica tu conexión a internet.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div style={containerStyle}>
      <h2 style={{ marginBottom: "1rem" }}>Crear Sesión</h2>

      {success && createdSession && (
        <div style={successStyle}>
          <p>Sesión creada correctamente.</p>
          <p style={{ marginTop: 8 }}><strong>PIN de la sesión:</strong></p>
          <code style={{
            display: "block", marginTop: 4, padding: "8px 12px",
            backgroundColor: "#0f3460", borderRadius: 4,
            fontSize: "1.4rem", letterSpacing: "4px", textAlign: "center",
          }}>
            {createdSession.pin}
          </code>
        </div>
      )}

      {submitError && <div style={errorMsgStyle}>{submitError}</div>}

      <form onSubmit={handleSubmit}>
        <div style={fieldGroupStyle}>
          <label htmlFor="session-name" style={labelStyle}>Nombre</label>
          <input id="session-name" type="text" value={name}
            onChange={(e) => setName(e.target.value)}
            style={inputStyle(Boolean(fieldErrors.name))}
            placeholder="Nombre de la sesión" />
          {fieldErrors.name && <p style={errorStyle}>{fieldErrors.name}</p>}
        </div>

        <div style={fieldGroupStyle}>
          <label style={labelStyle}>Misiones ({missions.length})</label>
          {missions.length > 0 && (
            <div data-testid="stage-list" style={{ marginBottom: 8 }}>
              {missions.map((m, i) => (
                <div key={m.missionId} style={{ ...missionCardStyle, flexDirection: "column", gap: 8 }}>
                  <div style={{ display: "flex", alignItems: "flex-start", gap: 10 }}>
                    <span style={orderBadgeStyle}>{i + 1}</span>
                    <div style={{ flex: 1, minWidth: 0 }}>
                      <div style={{ fontWeight: 600, fontSize: "0.9rem" }}>{m.missionTitle}</div>
                      <div style={{ color: "#999", fontSize: "0.78rem", marginTop: 2 }}>
                        {m.missionType === "Treasure"
                          ? `${m.timeMinutes === -1 ? "10 seg (prueba)" : `${m.timeMinutes} min`} · ${m.stageCount} etapa${m.stageCount !== 1 ? "s" : ""}`
                          : m.quizId
                            ? `${m.timeMinutes} min · ${m.questionCount} preg. · ${m.quizTitle}`
                            : "Trivia · seleccionar quiz"}
                      </div>
                    </div>
                    <button
                      type="button"
                      onClick={() => removeMission(m.missionId)}
                      style={removeBtnStyle}
                      aria-label={`Quitar misión ${m.missionTitle}`}
                    >
                      Quitar
                    </button>
                  </div>
                  {m.missionType === "Trivia" && (
                    <div style={{ paddingLeft: 38 }}>
                      <select
                        value={m.quizId ?? ""}
                        onChange={e => selectQuizForMission(m.missionId, e.target.value)}
                        style={{
                          width: "100%", padding: "6px 8px", borderRadius: 4,
                          border: `1px solid ${!m.quizId ? "#e94560" : "#0f3460"}`,
                          backgroundColor: "#0d1b35", color: "white", fontSize: "0.82rem",
                        }}
                      >
                        <option value="">— Seleccionar quiz —</option>
                        {quizzes.map(q => (
                          <option key={q.id} value={q.id}>{q.title} ({q.questionCount} preg.)</option>
                        ))}
                      </select>
                      {!m.quizId && (
                        <p style={{ color: "#e94560", fontSize: "0.75rem", margin: "4px 0 0" }}>
                          Requerido para crear la sesión
                        </p>
                      )}
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
          {addingMission && (
            <div style={{ color: "#999", fontSize: "0.85rem", marginBottom: 8 }}>
              Cargando misión...
            </div>
          )}

          <div style={{ position: "relative" }} ref={dropdownRef}>
            <label htmlFor="session-mission" style={labelStyle}>Agregar misión</label>
            <input id="session-mission" type="text"
              value={missionSearch}
              onChange={(e) => searchMissions(e.target.value)}
              onKeyDown={(e) => { if (e.key === "Enter") e.preventDefault(); }}
              style={inputStyle(Boolean(fieldErrors.stages))}
              placeholder="Buscar misión por nombre..."
              autoComplete="off" />
            {fieldErrors.stages && <p style={errorStyle}>{fieldErrors.stages}</p>}
            {showDropdown && (
              <ul style={dropdownStyle}>
                {searching && (
                  <li style={{ ...dropdownItemStyle, color: "#999" }}>Buscando...</li>
                )}
                {!searching && searchError && (
                  <li style={{ ...dropdownItemStyle, color: "#e94560" }}>
                    Error al buscar misiones. Verifica la conexión.
                  </li>
                )}
                {!searching && !searchError && missionResults.length === 0 && (
                  <li style={{ ...dropdownItemStyle, color: "#999" }}>
                    No hay misiones activas con ese nombre.
                  </li>
                )}
                {!searching && missionResults.map(m => (
                  <li key={m.id} style={dropdownItemStyle}
                    onMouseDown={(e) => { e.preventDefault(); e.stopPropagation(); addMission(m); }}
                    onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = "#0f3460")}
                    onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = "transparent")}>
                    <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 8 }}>
                      <span style={{ fontWeight: 600 }}>{m.title}</span>
                      <span style={{ color: "#999", fontSize: "0.78rem", flexShrink: 0 }}>
                        {m.difficulty} · {m.type}
                      </span>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>

        <button type="submit" disabled={isSubmitting} style={submitBtnStyle(isSubmitting)}>
          {isSubmitting ? "Creando..." : "Crear Sesión"}
        </button>
      </form>
    </div>
  );
}
