import { useState, type FormEvent } from "react";
import { crearOperador, ApiError } from "../../services/operadorApi";

interface FieldErrors {
  name?: string;
  email?: string;
  password?: string;
}

function validateName(value: string): string | undefined {
  if (!value.trim()) return "El nombre es obligatorio.";
  if (value.trim().length > 100) return "El nombre no puede exceder los 100 caracteres.";
  return undefined;
}

function validateEmail(value: string): string | undefined {
  if (!value.trim()) return "El email es obligatorio.";
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim()))
    return "Formato de email inválido.";
  return undefined;
}

function validatePassword(value: string): string | undefined {
  if (!value) return "La contraseña es obligatoria.";
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

export function CrearOperador() {
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [success, setSuccess] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitError(null);
    setSuccess(false);

    const errors: FieldErrors = {
      name: validateName(name),
      email: validateEmail(email),
      password: validatePassword(password),
    };
    setFieldErrors(errors);

    if (Object.values(errors).some(Boolean)) return;

    setIsSubmitting(true);

    try {
      await crearOperador({
        name: name.trim(),
        email: email.trim(),
        password,
      });
      setSuccess(true);
      setName("");
      setEmail("");
      setPassword("");
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 409) {
          setSubmitError("El email ya está registrado.");
        } else if (err.status === 400) {
          setSubmitError("Datos inválidos. Verificá los campos.");
        } else {
          setSubmitError("Error al crear el operador. Intentalo de nuevo.");
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
      <h2 style={{ marginBottom: "1rem" }}>Crear Cuenta de Operador</h2>

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
          Operador creado correctamente.
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
          <label htmlFor="op-name" style={labelStyle}>
            Nombre
          </label>
          <input
            id="op-name"
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            style={inputStyle(Boolean(fieldErrors.name))}
            autoComplete="name"
          />
          {fieldErrors.name && <p style={errorStyle}>{fieldErrors.name}</p>}
        </div>

        <div style={fieldGroupStyle}>
          <label htmlFor="op-email" style={labelStyle}>
            Email
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

        <div style={{ ...fieldGroupStyle, marginBottom: 20 }}>
          <label htmlFor="op-password" style={labelStyle}>
            Contraseña
          </label>
          <input
            id="op-password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            style={inputStyle(Boolean(fieldErrors.password))}
            autoComplete="new-password"
          />
          {fieldErrors.password && (
            <p style={errorStyle}>{fieldErrors.password}</p>
          )}
        </div>

        <button type="submit" disabled={isSubmitting} style={submitBtnStyle(isSubmitting)}>
          {isSubmitting ? "Creando..." : "Crear Operador"}
        </button>
      </form>
    </div>
  );
}
