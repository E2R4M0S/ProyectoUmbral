import { useState, type FormEvent } from "react";
import { desactivarOperador, ApiError } from "../../services/operadorApi";

interface FieldErrors {
  email?: string;
}

function validateEmail(value: string): string | undefined {
  if (!value.trim()) return "El email es obligatorio.";
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim()))
    return "Formato de email inválido.";
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
  backgroundColor: disabled ? "#999" : "#dc3545",
  color: "#fff",
  border: "none",
  borderRadius: 4,
  cursor: disabled ? "not-allowed" : "pointer",
  fontSize: 16,
  fontWeight: 600,
});

export function DesactivarOperador() {
  const [email, setEmail] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [success, setSuccess] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitError(null);
    setSuccess(false);

    const errors: FieldErrors = {
      email: validateEmail(email),
    };
    setFieldErrors(errors);

    if (Object.values(errors).some(Boolean)) return;

    const confirmed = window.confirm(`¿Desactivar operador ${email.trim()}? Esta acción revocará todas sus sesiones activas.`);
    if (!confirmed) return;

    setIsSubmitting(true);

    try {
      await desactivarOperador({ email: email.trim() });
      setSuccess(true);
      setEmail("");
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 404) {
          setSubmitError("No se encontró un operador con ese email.");
        } else if (err.status === 400) {
          setSubmitError("Datos inválidos. Verificá el email.");
        } else {
          setSubmitError("Error al desactivar el operador. Intentalo de nuevo.");
        }
      } else {
        setSubmitError("Error de conexión. Verificá tu conexión a internet.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div style={{ maxWidth: 400, margin: "0 auto", padding: "1rem" }}>
      <h2 style={{ marginBottom: "1rem" }}>Desactivar Cuenta de Operador</h2>

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
          Operador desactivado correctamente.
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
          <label htmlFor="op-email" style={labelStyle}>
            Email del Operador
          </label>
          <input
            id="op-email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            style={inputStyle(Boolean(fieldErrors.email))}
            autoComplete="email"
          />
          {fieldErrors.email && <p style={errorStyle}>{fieldErrors.email}</p>}
        </div>

        <button type="submit" disabled={isSubmitting} style={submitBtnStyle(isSubmitting)}>
          {isSubmitting ? "Desactivando..." : "Desactivar Operador"}
        </button>
      </form>
    </div>
  );
}