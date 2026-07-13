import { useState, useEffect, type FormEvent } from "react";
import { createTeam, ApiError } from "../../services/teamsApi";
import { fetchWithAuth } from "../../services/api";

interface Participant {
  id: string;
  name: string;
  email: string;
}

interface FieldErrors {
  name?: string;
  description?: string;
  leaderId?: string;
}

function validateName(value: string): string | undefined {
  if (!value.trim()) return "El nombre es obligatorio.";
  if (value.trim().length > 100) return "El nombre no puede exceder los 100 caracteres.";
  return undefined;
}

function validateDescription(value: string): string | undefined {
  if (!value.trim()) return "La descripción es obligatoria.";
  if (value.trim().length > 500) return "La descripción no puede exceder los 500 caracteres.";
  return undefined;
}

function validateLeaderId(value: string): string | undefined {
  if (!value.trim()) return "El ID del líder es obligatorio.";
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

export function CrearEquipo() {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [leaderId, setLeaderId] = useState("");
  const [participants, setParticipants] = useState<Participant[]>([]);
  const [loadingParticipants, setLoadingParticipants] = useState(true);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [success, setSuccess] = useState(false);
  const [createdTeam, setCreatedTeam] = useState<{ id: string; joinCode: string } | null>(null);

  useEffect(() => {
    fetchParticipants();
  }, []);

  async function fetchParticipants() {
    try {
      const resp = await fetchWithAuth("/api/admin/users?role=participant&pageSize=100");
      if (resp.ok) {
        const data = await resp.json();
        setParticipants(data.items ?? []);
      }
    } catch { /* ignore */ }
    finally { setLoadingParticipants(false); }
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitError(null);
    setSuccess(false);

      const errors: FieldErrors = {
        name: validateName(name),
        description: validateDescription(description),
        leaderId: validateLeaderId(leaderId),
      };
    setFieldErrors(errors);

    if (Object.values(errors).some(Boolean)) return;

    setIsSubmitting(true);

    try {
      const result = await createTeam({
        name: name.trim(),
        description: description.trim(),
        leaderId: leaderId,
      });
      setSuccess(true);
      setCreatedTeam({ id: result.id, joinCode: result.joinCode });
      setName("");
      setDescription("");
      setLeaderId("");
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 400) {
          setSubmitError("Datos inválidos. Verificá los campos.");
        } else if (err.status === 409) {
          setSubmitError("Ya existe un equipo con ese nombre. Elegí otro.");
        } else {
          setSubmitError("Error al crear el equipo. Intentalo de nuevo.");
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
      <h2 style={{ marginBottom: "1rem" }}>Crear Equipo</h2>

      {success && createdTeam && (
        <div style={successStyle}>
          <p>Equipo creado correctamente.</p>
          <p style={{ marginTop: 8 }}>
            <strong>Código de unión:</strong>
          </p>
          <code style={{
            display: "block",
            marginTop: 4,
            padding: "8px 12px",
            backgroundColor: "#0f3460",
            borderRadius: 4,
            fontSize: "1.2rem",
            letterSpacing: "2px",
          }}>
            {createdTeam.joinCode}
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
          <label htmlFor="team-name" style={labelStyle}>
            Nombre
          </label>
          <input
            id="team-name"
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            style={inputStyle(Boolean(fieldErrors.name))}
          />
          {fieldErrors.name && <p style={errorStyle}>{fieldErrors.name}</p>}
        </div>

        <div style={fieldGroupStyle}>
          <label htmlFor="team-description" style={labelStyle}>
            Descripción
          </label>
          <textarea
            id="team-description"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            rows={3}
            style={{ ...inputStyle(Boolean(fieldErrors.description)), resize: "vertical" }}
          />
          {fieldErrors.description && <p style={errorStyle}>{fieldErrors.description}</p>}
        </div>

        <div style={{ ...fieldGroupStyle, marginBottom: 20 }}>
          <label htmlFor="team-leader" style={labelStyle}>
            Líder (participante)
          </label>
          {loadingParticipants ? (
            <p style={{ color: "#999", fontSize: 14 }}>Cargando participantes...</p>
          ) : (
            <select
              id="team-leader"
              value={leaderId}
              onChange={(e) => setLeaderId(e.target.value)}
              style={inputStyle(Boolean(fieldErrors.leaderId))}
            >
              <option value="">-- Seleccionar participante --</option>
              {participants.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name} ({p.email})
                </option>
              ))}
            </select>
          )}
          {fieldErrors.leaderId && <p style={errorStyle}>{fieldErrors.leaderId}</p>}
        </div>

        <button type="submit" disabled={isSubmitting} style={submitBtnStyle(isSubmitting)}>
          {isSubmitting ? "Creando..." : "Crear Equipo"}
        </button>
      </form>
    </div>
  );
}