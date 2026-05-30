import { useState, useEffect, useRef, type FormEvent } from "react";
import { createSession, ApiError } from "../../services/sessionsApi";
import { listMissions } from "../../services/missionsApi";
import type { MissionListItem } from "../../types/mission";

interface FieldErrors {
  name?: string;
  missionId?: string;
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

export function CrearSesion() {
  const [name, setName] = useState("");
  const [missionId, setMissionId] = useState("");
  const [missionTitle, setMissionTitle] = useState("");
  const [missionSearch, setMissionSearch] = useState("");
  const [missionResults, setMissionResults] = useState<MissionListItem[]>([]);
  const [showDropdown, setShowDropdown] = useState(false);
  const [searching, setSearching] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [success, setSuccess] = useState(false);
  const [createdSession, setCreatedSession] = useState<{ id: string; pin: string } | null>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setShowDropdown(false);
      }
    }
    document.addEventListener("mousedown", handleClick);
    return () => document.removeEventListener("mousedown", handleClick);
  }, []);

  async function searchMissions(query: string) {
    setMissionSearch(query);
    if (query.trim().length < 2) { setMissionResults([]); setShowDropdown(false); return; }
    setSearching(true);
    try {
      const result = await listMissions({ search: query, status: "Active", page: 1, pageSize: 10 });
      setMissionResults(result.items);
      setShowDropdown(result.items.length > 0);
    } catch { setMissionResults([]); }
    finally { setSearching(false); }
  }

  function selectMission(item: MissionListItem) {
    setMissionId(item.id);
    setMissionTitle(item.title);
    setMissionSearch(item.title);
    setShowDropdown(false);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitError(null);
    setSuccess(false);

    const errors: FieldErrors = { name: validateName(name) };
    if (!missionId) errors.missionId = "Seleccioná una misión.";
    setFieldErrors(errors);

    if (Object.values(errors).some(Boolean)) return;

    setIsSubmitting(true);

    try {
      const result = await createSession({
        name: name.trim(),
        missionId: missionId.trim(),
        missionTitle: missionTitle,
      });
      setSuccess(true);
      setCreatedSession({ id: result.id, pin: result.pin });
      setName("");
      setMissionId("");
      setMissionSearch("");
      setMissionTitle("");
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 400) setSubmitError("Datos inválidos. Verificá los campos.");
        else if (err.status === 404) setSubmitError("Misión no encontrada.");
        else setSubmitError("Error al crear la sesión. Intentalo de nuevo.");
      } else {
        setSubmitError("Error de conexión. Verificá tu conexión a internet.");
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

        <div style={{ ...fieldGroupStyle, marginBottom: 20, position: "relative" }} ref={dropdownRef}>
          <label htmlFor="session-mission" style={labelStyle}>Misión</label>
          <input id="session-mission" type="text"
            value={missionSearch}
            onChange={(e) => searchMissions(e.target.value)}
            style={inputStyle(Boolean(fieldErrors.missionId))}
            placeholder="Buscar misión por nombre..."
            autoComplete="off" />
          {fieldErrors.missionId && <p style={errorStyle}>{fieldErrors.missionId}</p>}
          {missionId && <p style={{ color: "#28a745", fontSize: 12, marginTop: 4 }}>✓ {missionTitle}</p>}
          {showDropdown && (
            <ul style={dropdownStyle}>
              {searching && <li style={{ ...dropdownItemStyle, color: "#999" }}>Buscando...</li>}
              {missionResults.map(m => (
                <li key={m.id} style={dropdownItemStyle}
                  onMouseDown={() => selectMission(m)}
                  onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = "#0f3460")}
                  onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = "transparent")}>
                  <span style={{ fontWeight: 600 }}>{m.title}</span>
                  <span style={{ color: "#999", marginLeft: 8, fontSize: "0.8rem" }}>
                    {m.difficulty} · {m.type} · {m.status === "Active" ? "Activa" : "Borrador"}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>

        <button type="submit" disabled={isSubmitting} style={submitBtnStyle(isSubmitting)}>
          {isSubmitting ? "Creando..." : "Crear Sesión"}
        </button>
      </form>
    </div>
  );
}
