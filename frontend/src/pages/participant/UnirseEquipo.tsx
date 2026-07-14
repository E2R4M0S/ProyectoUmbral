import { useState, type FormEvent } from "react";
import { joinTeam, ApiError } from "../../services/teamsApi";

function validateJoinCode(value: string): string | undefined {
  if (!value.trim()) return "El código de unión es obligatorio.";
  if (value.trim().length !== 6) return "El código debe tener 6 caracteres.";
  return undefined;
}

export function UnirseEquipo() {
  const [joinCode, setJoinCode] = useState("");
  const [codeError, setCodeError] = useState<string>();
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [success, setSuccess] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitError(null);
    setSuccess(false);

    const error = validateJoinCode(joinCode);
    if (error) { setCodeError(error); return; }
    setCodeError(undefined);

    setIsSubmitting(true);
    try {
      await joinTeam(joinCode.trim().toUpperCase());
      setSuccess(true);
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 403) setSubmitError("Código inválido. Verifica el código.");
        else if (err.status === 409) setSubmitError("Ya estás en este equipo.");
        else setSubmitError("Error al unirse. Inténtalo de nuevo.");
      } else {
        setSubmitError("Error de conexión.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="code-entry-page">
      <h2>Unirse a un Equipo</h2>
      <p className="page-subtitle">
        Ingresa el código de 6 caracteres que te compartió el líder
      </p>

      {success && <div className="alert alert-success" style={{ marginBottom: "1rem" }}>¡Te uniste al equipo correctamente!</div>}
      {submitError && <div className="alert alert-error" style={{ marginBottom: "1rem" }}>{submitError}</div>}

      <form onSubmit={handleSubmit}>
        <input
          type="text"
          maxLength={6}
          placeholder="ABC123"
          value={joinCode}
          onChange={(e) => setJoinCode(e.target.value.toUpperCase())}
          className={`code-input-field${codeError ? " has-error" : ""}`}
          autoFocus
        />
        {codeError && <p className="code-field-error">{codeError}</p>}
        <button
          type="submit"
          disabled={isSubmitting}
          className="btn btn-primary"
          style={{ width: "100%", marginTop: "1rem" }}
        >
          {isSubmitting ? "Uniéndose..." : "Unirse al Equipo"}
        </button>
      </form>
    </div>
  );
}
