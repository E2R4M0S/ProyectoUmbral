import { useState, useEffect, type FormEvent } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { getTeamById, updateTeam, ApiError } from "../../services/teamsApi";
import type { TeamDetail } from "../../types/team";

interface FieldErrors {
  name?: string;
  description?: string;
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

const inputStyle = (hasError: boolean): React.CSSProperties => ({
  width: "100%",
  padding: 8,
  border: hasError ? "1px solid #e94560" : "1px solid #0f3460",
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

export function EditarEquipo() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [success, setSuccess] = useState(false);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function loadTeam() {
      if (!id) return;
      try {
        const team: TeamDetail = await getTeamById(id);
        setName(team.name);
        setDescription(team.description);
      } catch {
        setSubmitError("No se pudo cargar el equipo.");
      } finally {
        setLoading(false);
      }
    }
    loadTeam();
  }, [id]);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitError(null);
    setSuccess(false);

    const errors: FieldErrors = {
      name: validateName(name),
      description: validateDescription(description),
    };
    setFieldErrors(errors);

    if (Object.values(errors).some(Boolean)) return;

    setIsSubmitting(true);

    try {
      await updateTeam(id!, {
        name: name.trim(),
        description: description.trim(),
      });
      setSuccess(true);
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 400) {
          setSubmitError("Datos inválidos. Verificá los campos.");
        } else {
          setSubmitError("Error al actualizar el equipo. Intentalo de nuevo.");
        }
      } else {
        setSubmitError("Error de conexión. Verificá tu conexión a internet.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  if (loading) {
    return <div style={{ ...containerStyle, color: "#aaa" }}>Cargando...</div>;
  }

  return (
    <div style={containerStyle}>
      <h2 style={{ marginBottom: "1rem" }}>Editar Equipo</h2>

      {success && (
        <div style={successStyle}>
          Equipo actualizado correctamente.
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

        <div style={{ ...fieldGroupStyle, marginBottom: 20 }}>
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

        <button type="submit" disabled={isSubmitting} style={submitBtnStyle(isSubmitting)}>
          {isSubmitting ? "Guardando..." : "Guardar Cambios"}
        </button>
      </form>

      <button
        onClick={() => navigate(`/admin/equipos/${id}`)}
        style={{
          width: "100%",
          marginTop: 8,
          padding: 10,
          backgroundColor: "transparent",
          color: "#aaa",
          border: "1px solid #666",
          borderRadius: 4,
          cursor: "pointer",
          fontSize: 16,
        }}
      >
        Cancelar
      </button>
    </div>
  );
}