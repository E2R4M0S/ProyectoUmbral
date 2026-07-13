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

function validate(title: string, description: string, difficulty: string, timeMinutes: string, type: string): FieldErrors {
  const e: FieldErrors = {};
  if (!title.trim())                         e.title       = "El título es obligatorio.";
  else if (title.trim().length > 200)        e.title       = "El título no puede exceder los 200 caracteres.";
  if (!description.trim())                   e.description = "La descripción es obligatoria.";
  else if (description.trim().length > 2000) e.description = "La descripción no puede exceder los 2000 caracteres.";
  if (!difficulty)                           e.difficulty  = "La dificultad es obligatoria.";
  else if (!["Easy","Medium","Hard"].includes(difficulty)) e.difficulty = "Dificultad inválida.";
  if (type === "Treasure" && ![-1,15,30,60,90].includes(Number(timeMinutes)))
    e.timeMinutes = "El tiempo debe ser 15, 30, 60 o 90 minutos.";
  if (!type)                                 e.type        = "El tipo es obligatorio.";
  else if (!["Treasure","Trivia"].includes(type)) e.type = "Tipo inválido.";
  return e;
}

export function CrearMision() {
  const [title, setTitle]             = useState("");
  const [description, setDesc]        = useState("");
  const [difficulty, setDiff]         = useState("");
  const [timeMinutes, setTime]        = useState("");
  const [type, setType]               = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitting, setSub]          = useState(false);
  const [success, setSuccess]         = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitError(null);
    setSuccess(false);

    const errors = validate(title, description, difficulty, timeMinutes, type);
    setFieldErrors(errors);
    if (Object.values(errors).some(Boolean)) return;

    setSub(true);
    try {
      await createMission({
        title: title.trim(),
        description: description.trim(),
        difficulty: difficulty as Difficulty,
        timeMinutes: type === "Trivia" ? 0 : Number(timeMinutes),
        type: type as MissionType,
      });
      setSuccess(true);
      setTitle(""); setDesc(""); setDiff(""); setTime(""); setType("");
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 409)      setSubmitError("Ya existe una misión con ese título.");
        else if (err.status === 400) setSubmitError("Datos inválidos. Verificá los campos.");
        else                         setSubmitError("Error al crear la misión. Intentalo de nuevo.");
      } else {
        setSubmitError("Error de conexión. Verificá tu conexión a internet.");
      }
    } finally {
      setSub(false);
    }
  }

  return (
    <div className="page" style={{ maxWidth: 560 }}>
      <div className="page-header">
        <h1 className="page-title">Crear Misión</h1>
      </div>

      <div className="card">
        {success && <div className="alert alert-success" style={{ marginBottom: "1.25rem" }}>Misión creada correctamente.</div>}
        {submitError && <div className="alert alert-error" style={{ marginBottom: "1.25rem" }}>{submitError}</div>}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label className="form-label" htmlFor="mission-title">Título</label>
            <input
              id="mission-title"
              type="text"
              className="form-input"
              style={fieldErrors.title ? { borderColor: "var(--color-error)" } : {}}
              value={title}
              onChange={(e) => setTitle(e.target.value)}
            />
            {fieldErrors.title && <span className="form-hint" style={{ color: "var(--color-error)" }}>{fieldErrors.title}</span>}
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="mission-desc">Descripción</label>
            <textarea
              id="mission-desc"
              className="form-textarea"
              style={fieldErrors.description ? { borderColor: "var(--color-error)" } : {}}
              value={description}
              onChange={(e) => setDesc(e.target.value)}
              rows={4}
            />
            {fieldErrors.description && <span className="form-hint" style={{ color: "var(--color-error)" }}>{fieldErrors.description}</span>}
          </div>

          <div style={{ display: "grid", gridTemplateColumns: type === "Treasure" ? "1fr 1fr" : "1fr", gap: "1rem" }}>
            <div className="form-group">
              <label className="form-label" htmlFor="mission-difficulty">Dificultad</label>
              <select
                id="mission-difficulty"
                className="form-select"
                style={fieldErrors.difficulty ? { borderColor: "var(--color-error)" } : {}}
                value={difficulty}
                onChange={(e) => setDiff(e.target.value)}
              >
                <option value="">Seleccionar...</option>
                <option value="Easy">Fácil</option>
                <option value="Medium">Media</option>
                <option value="Hard">Difícil</option>
              </select>
              {fieldErrors.difficulty && <span className="form-hint" style={{ color: "var(--color-error)" }}>{fieldErrors.difficulty}</span>}
            </div>

            {type === "Treasure" && (
              <div className="form-group">
                <label className="form-label" htmlFor="mission-time">Tiempo</label>
                <select
                  id="mission-time"
                  className="form-select"
                  style={fieldErrors.timeMinutes ? { borderColor: "var(--color-error)" } : {}}
                  value={timeMinutes}
                  onChange={(e) => setTime(e.target.value)}
                >
                  <option value="">Seleccionar...</option>
                  <option value="-1">10 seg (prueba)</option>
                  <option value="15">15 min</option>
                  <option value="30">30 min</option>
                  <option value="60">60 min</option>
                  <option value="90">90 min</option>
                </select>
                {fieldErrors.timeMinutes && <span className="form-hint" style={{ color: "var(--color-error)" }}>{fieldErrors.timeMinutes}</span>}
              </div>
            )}
          </div>

          <div className="form-group" style={{ marginBottom: "1.5rem" }}>
            <label className="form-label" htmlFor="mission-type">Tipo</label>
            <select
              id="mission-type"
              className="form-select"
              style={fieldErrors.type ? { borderColor: "var(--color-error)" } : {}}
              value={type}
              onChange={(e) => setType(e.target.value)}
            >
              <option value="">Seleccionar...</option>
              <option value="Treasure">Búsqueda del Tesoro</option>
              <option value="Trivia">Trivia</option>
            </select>
            {fieldErrors.type && <span className="form-hint" style={{ color: "var(--color-error)" }}>{fieldErrors.type}</span>}
          </div>

          <button type="submit" className="btn btn-primary" style={{ width: "100%" }} disabled={submitting}>
            {submitting ? "Creando..." : "Crear Misión"}
          </button>
        </form>
      </div>
    </div>
  );
}
