import { useState, type FormEvent } from "react";
import { createSession, ApiError } from "../../services/sessionsApi";

interface FieldErrors {
  name?: string;
  missionId?: string;
}

function validateName(value: string): string | undefined {
  if (!value.trim()) return "El nombre es obligatorio.";
  if (value.trim().length > 100) return "El nombre no puede exceder los 100 caracteres.";
  return undefined;
}

function validateMissionId(value: string): string | undefined {
  if (!value.trim()) return "El ID de la misión es obligatorio.";
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

export function CrearSesion() {
  const [name, setName] = useState("");
  const [missionId, setMissionId] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [success, setSuccess] = useState(false);
  const [createdSession, setCreatedSession] = useState<{ id: string; pin: string } | null>(null);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitError(null);
    setSuccess(false);

    const errors: FieldErrors = {
      name: validateName(name),
      missionId: validateMissionId(missionId),
    };
    setFieldErrors(errors);

    if (Object.values(errors).some(Boolean)) return;

    setIsSubmitting(true);

    try {
      const result = await createSession({
        name: name.trim(),
        missionId: missionId.trim(),
      });
      setSuccess(true);
      setCreatedSession({ id: result.id, pin: result.pin });
      setName("");
      setMissionId("");
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 400) {
          setSubmitError("Datos inválidos. Verificá los campos.");
        } else if (err.status === 404) {
          setSubmitError("Misión no encontrada.");
        } else {
          setSubmitError("Error al crear la sesión. Intentalo de nuevo.");
        }
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
          <p style={{ marginTop: 8 }}>
            <strong>PIN de la sesión:</strong>
          </p>
          <code style={{
            display: "block",
            marginTop: 4,
            padding: "8px 12px",
            backgroundColor: "#0f3460",
            borderRadius: 4,
            fontSize: "1.4rem",
            letterSpacing: "4px",
            textAlign: "center",
          }}>
            {createdSession.pin}
          </code>
        </div>
      )}

      {submitError && (
        <div style={errorMsgStyle}>
          {submitError}
        </div>
      )}

      <form onSubmit={handleSubmit}>
        <div style={fieldGroupStyle}>
          <label htmlFor="session-name" style={labelStyle}>
            Nombre
          </label>
          <input
            id="session-name"
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            style={inputStyle(Boolean(fieldErrors.name))}
            placeholder="Nombre de la sesión"
          />
          {fieldErrors.name && <p style={errorStyle}>{fieldErrors.name}</p>}
        </div>

        <div style={{ ...fieldGroupStyle, marginBottom: 20 }}>
          <label htmlFor="session-mission" style={labelStyle}>
            ID de Misión
          </label>
          <input
            id="session-mission"
            type="text"
            value={missionId}
            onChange={(e) => setMissionId(e.target.value)}
            style={inputStyle(Boolean(fieldErrors.missionId))}
            placeholder="UUID de la misión"
          />
          {fieldErrors.missionId && <p style={errorStyle}>{fieldErrors.missionId}</p>}
        </div>

        <button type="submit" disabled={isSubmitting} style={submitBtnStyle(isSubmitting)}>
          {isSubmitting ? "Creando..." : "Crear Sesión"}
        </button>
      </form>
    </div>
  );
}