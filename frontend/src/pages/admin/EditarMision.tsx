import { useState, useEffect, type FormEvent } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { getMissionById, updateMission, ApiError } from "../../services/missionsApi";
import type { MissionDetail, Difficulty, MissionType } from "../../types/mission";

interface FieldErrors {
  title?: string;
  description?: string;
  difficulty?: string;
  timeMinutes?: string;
  type?: string;
}

function validateTitle(value: string): string | undefined {
  if (!value.trim()) return "El título es obligatorio.";
  if (value.trim().length > 200) return "El título no puede exceder los 200 caracteres.";
  return undefined;
}

function validateDescription(value: string): string | undefined {
  if (!value.trim()) return "La descripción es obligatoria.";
  if (value.trim().length > 2000) return "La descripción no puede exceder los 2000 caracteres.";
  return undefined;
}

function validateDifficulty(value: string): string | undefined {
  if (!value) return "La dificultad es obligatoria.";
  if (!["Easy", "Medium", "Hard"].includes(value)) return "Dificultad inválida.";
  return undefined;
}

function validateTimeMinutes(value: number): string | undefined {
  if (![15, 30, 60, 90].includes(value)) return "El tiempo debe ser 15, 30, 60 o 90 minutos.";
  return undefined;
}

function validateType(value: string): string | undefined {
  if (!value) return "El tipo es obligatorio.";
  if (!["Treasure", "Trivia"].includes(value)) return "Tipo inválido.";
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

export function EditarMision() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [difficulty, setDifficulty] = useState("");
  const [timeMinutes, setTimeMinutes] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [success, setSuccess] = useState(false);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function loadMission() {
      if (!id) return;
      setLoading(true);
      try {
        const mission: MissionDetail = await getMissionById(id);
        setTitle(mission.title);
        setDescription(mission.description);
        setDifficulty(mission.difficulty);
        setTimeMinutes(String(mission.timeMinutes));
      } catch {
        setSubmitError("No se pudo cargar la misión.");
      } finally {
        setLoading(false);
      }
    }
    loadMission();
  }, [id]);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitError(null);
    setSuccess(false);

    const errors: FieldErrors = {
      title: validateTitle(title),
      description: validateDescription(description),
      difficulty: validateDifficulty(difficulty),
      timeMinutes: validateTimeMinutes(Number(timeMinutes)),
    };
    setFieldErrors(errors);

    if (Object.values(errors).some(Boolean)) return;

    setIsSubmitting(true);

    try {
      await updateMission(id!, {
        title: title.trim(),
        description: description.trim(),
        difficulty: difficulty as Difficulty,
        timeMinutes: Number(timeMinutes),
      } as any);
      setSuccess(true);
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 400) {
          setSubmitError("Datos inválidos. Verificá los campos.");
        } else {
          setSubmitError("Error al actualizar la misión. Intentalo de nuevo.");
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
      <h2 style={{ marginBottom: "1rem" }}>Editar Misión</h2>

      {success && (
        <div style={successStyle}>
          Misión actualizada correctamente.
        </div>
      )}

      {submitError && (
        <div style={errorMsgStyle}>
          {submitError}
        </div>
      )}

      <form onSubmit={handleSubmit}>
        <div style={fieldGroupStyle}>
          <label htmlFor="mission-title" style={labelStyle}>
            Título
          </label>
          <input
            id="mission-title"
            type="text"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            style={inputStyle(Boolean(fieldErrors.title))}
          />
          {fieldErrors.title && <p style={errorStyle}>{fieldErrors.title}</p>}
        </div>

        <div style={fieldGroupStyle}>
          <label htmlFor="mission-description" style={labelStyle}>
            Descripción
          </label>
          <textarea
            id="mission-description"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            rows={4}
            style={{ ...inputStyle(Boolean(fieldErrors.description)), resize: "vertical" }}
          />
          {fieldErrors.description && <p style={errorStyle}>{fieldErrors.description}</p>}
        </div>

        <div style={fieldGroupStyle}>
          <label htmlFor="mission-difficulty" style={labelStyle}>
            Dificultad
          </label>
          <select
            id="mission-difficulty"
            value={difficulty}
            onChange={(e) => setDifficulty(e.target.value)}
            style={inputStyle(Boolean(fieldErrors.difficulty))}
          >
            <option value="">Seleccionar dificultad</option>
            <option value="Easy">Fácil</option>
            <option value="Medium">Media</option>
            <option value="Hard">Difícil</option>
          </select>
          {fieldErrors.difficulty && <p style={errorStyle}>{fieldErrors.difficulty}</p>}
        </div>

        <div style={{ ...fieldGroupStyle, marginBottom: 20 }}>
          <label htmlFor="mission-time" style={labelStyle}>
            Tiempo (minutos)
          </label>
          <select
            id="mission-time"
            value={timeMinutes}
            onChange={(e) => setTimeMinutes(e.target.value)}
            style={inputStyle(Boolean(fieldErrors.timeMinutes))}
          >
            <option value="">Seleccionar tiempo</option>
            <option value="15">15 minutos</option>
            <option value="30">30 minutos</option>
            <option value="60">60 minutos</option>
            <option value="90">90 minutos</option>
          </select>
          {fieldErrors.timeMinutes && <p style={errorStyle}>{fieldErrors.timeMinutes}</p>}
        </div>

        <button type="submit" disabled={isSubmitting} style={submitBtnStyle(isSubmitting)}>
          {isSubmitting ? "Guardando..." : "Guardar Cambios"}
        </button>
      </form>

      <button
        onClick={() => navigate("/admin/misiones")}
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