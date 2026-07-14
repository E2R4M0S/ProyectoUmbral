import { useState, type FormEvent } from "react";
import { desactivarOperador, ApiError } from "../../services/operadorApi";

function validateEmail(v: string) {
  if (!v.trim()) return "El email es obligatorio.";
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v.trim())) return "Formato de email inválido.";
}

export function DesactivarOperador() {
  const [email, setEmail]       = useState("");
  const [emailErr, setEmailErr] = useState<string | undefined>();
  const [submitErr, setSubmitErr] = useState<string | null>(null);
  const [submitting, setSub]    = useState(false);
  const [success, setSuccess]   = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitErr(null);
    setSuccess(false);

    const err = validateEmail(email);
    setEmailErr(err);
    if (err) return;

    const confirmed = window.confirm(
      `¿Desactivar operador "${email.trim()}"? Esta acción revocará todas sus sesiones activas.`
    );
    if (!confirmed) return;

    setSub(true);
    try {
      await desactivarOperador({ email: email.trim() });
      setSuccess(true);
      setEmail("");
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 404)      setSubmitErr("No se encontró un operador con ese email.");
        else if (err.status === 400) setSubmitErr("Datos inválidos. Verifica el email.");
        else                         setSubmitErr("Error al desactivar el operador. Inténtalo de nuevo.");
      } else {
        setSubmitErr("Error de conexión. Verifica tu conexión a internet.");
      }
    } finally {
      setSub(false);
    }
  }

  return (
    <div className="page" style={{ maxWidth: 480 }}>
      <div className="page-header">
        <h1 className="page-title">Desactivar Operador</h1>
      </div>

      <div className="card">
        <p style={{ color: "var(--text-muted)", fontSize: "0.875rem", marginBottom: "1.5rem" }}>
          Ingresa el email del operador que deseas desactivar. Se revocarán todos sus accesos activos.
        </p>

        {success && (
          <div className="alert alert-success" style={{ marginBottom: "1.25rem" }}>
            Operador desactivado correctamente.
          </div>
        )}
        {submitErr && (
          <div className="alert alert-error" style={{ marginBottom: "1.25rem" }}>
            {submitErr}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div className="form-group" style={{ marginBottom: "1.5rem" }}>
            <label className="form-label" htmlFor="op-email">Email del Operador</label>
            <input
              id="op-email"
              type="email"
              className="form-input"
              style={emailErr ? { borderColor: "var(--color-error)" } : {}}
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              autoComplete="email"
              placeholder="operador@ucab.edu.ve"
            />
            {emailErr && <span className="form-hint" style={{ color: "var(--color-error)" }}>{emailErr}</span>}
          </div>

          <button type="submit" className="btn btn-danger" style={{ width: "100%" }} disabled={submitting}>
            {submitting ? "Desactivando..." : "Desactivar Operador"}
          </button>
        </form>
      </div>
    </div>
  );
}
