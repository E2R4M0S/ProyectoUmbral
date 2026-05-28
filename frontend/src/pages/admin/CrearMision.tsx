import { useState, type FormEvent } from "react";
import { createMission, ApiError } from "../../services/missionsApi";
import type { Difficulty, MissionType } from "../../types/mission";

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
  border: hasError ? "1px solid #dc3545" : "1px solid #ccc",
  borderRadius: 4,
  boxSizing: "border-box",
});

const labelStyle: React.CSSProperties = {
  display: "block",
  marginBottom: 4,
  fontWeight: 600,
};

const errorStyle: React.CSSProperties = {
  color: "#dc3545",
  fontSize: 12,
  margin: "4px 0 0",
};

const fieldGroupStyle: React.CSSProperties = {
  marginBottom: 14,
};

const submitBtnStyle = (disabled: boolean): React.CSSProperties => ({
  width: "100%",
  padding: 10,
  backgroundColor: disabled ? "#999" : "#007bff",
  color: "#fff",
  border: "none",
  borderRadius: 4,
  cursor: disabled ? "not-allowed" : "pointer",
  fontSize: 16,
  fontWeight: 600,
});

export function CrearMision() {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [difficulty, setDifficulty] = useState("");
  const [timeMinutes, setTimeMinutes] = useState("");
  const [type, setType] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [success, setSuccess] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitError(null);
    setSuccess(false);

    const errors: FieldErrors = {
      title: validateTitle(title),
      description: validateDescription(description),
      difficulty: validateDifficulty(difficulty),
      timeMinutes: validateTimeMinutes(Number(timeMinutes)),
      type: validateType(type),
    };
    setFieldErrors(errors);

    if (Object.values(errors).some(Boolean)) return;

    setIsSubmitting(true);

    try {
      await createMission({
        title: title.trim(),
        description: description.trim(),
        difficulty: difficulty as Difficulty,
        timeMinutes: Number(timeMinutes),
        type: type as MissionType,
      });
      setSuccess(true);
      setTitle("");
      setDescription("");
      setDifficulty("");
      setTimeMinutes("");
      setType("");
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 409) {
          setSubmitError("Ya existe una misión con ese título.");
        } else if (err.status === 400) {
          setSubmitError("Datos inválidos. Verificá los campos.");
        } else {
          setSubmitError("Error al crear la misión. Intentalo de nuevo.");
        }
      } else {
        setSubmitError("Error de conexión. Verificá tu conexión a internet.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div style={{ maxWidth: 500, margin: "0 auto", padding: "1rem" }}>
      <h2 style={{ marginBottom: "1rem" }}>Crear Misión</h2>

      {success && (
        <div
          style={{
            marginBottom: 16,
            padding: "8px 12px",
            border: "1px solid #28a745",
            borderRadius: 4,
            backgroundColor: "#d4edda",
            color: "#155724",
          }}
        >
          Misión creada correctamente.
        </div>
      )}

      {submitError && (
        <div
          style={{
            color: "#dc3545",
            marginBottom: 16,
            padding: "8px 12px",
            border: "1px solid #dc3545",
            borderRadius: 4,
            backgroundColor: "#fff5f5",
          }}
        >
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

        <div style={fieldGroupStyle}>
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

        <div style={{ ...fieldGroupStyle, marginBottom: 20 }}>
          <label htmlFor="mission-type" style={labelStyle}>
            Tipo
          </label>
          <select
            id="mission-type"
            value={type}
            onChange={(e) => setType(e.target.value)}
            style={inputStyle(Boolean(fieldErrors.type))}
          >
            <option value="">Seleccionar tipo</option>
            <option value="Treasure">Tesoro</option>
            <option value="Trivia">Trivia</option>
          </select>
          {fieldErrors.type && <p style={errorStyle}>{fieldErrors.type}</p>}
        </div>

        <button type="submit" disabled={isSubmitting} style={submitBtnStyle(isSubmitting)}>
          {isSubmitting ? "Creando..." : "Crear Misión"}
        </button>
      </form>
    </div>
  );
}