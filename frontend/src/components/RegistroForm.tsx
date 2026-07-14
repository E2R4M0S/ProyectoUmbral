import { useState, type FormEvent } from "react";
import { registrarParticipante, ApiError } from "../services/registroApi";

interface RegistroFormProps {
  onSuccess: () => void;
}

interface FieldErrors {
  name?: string;
  alias?: string;
  email?: string;
  password?: string;
}

// ── Validación del lado del cliente ──────────────────────────────────────────

function validateName(value: string): string | undefined {
  if (!value.trim()) return "El nombre es obligatorio.";
  return undefined;
}

function validateAlias(value: string): string | undefined {
  if (!value.trim()) return "El alias es obligatorio.";
  if (value.trim().length < 3) return "El alias debe tener al menos 3 caracteres.";
  if (value.trim().length > 50) return "El alias no puede exceder los 50 caracteres.";
  if (!/^[a-zA-Z0-9_]+$/.test(value.trim()))
    return "El alias solo puede contener letras, números y guiones bajos.";
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
  if (value.length < 8) return "La contraseña debe tener al menos 8 caracteres.";
  return undefined;
}

// ── Estilos inline ───────────────────────────────────────────────────────────

const inputStyle = (hasError: boolean): React.CSSProperties => ({
  width: "100%",
  padding: 10,
  border: hasError ? "2px solid #e94560" : "1px solid #0f3460",
  borderRadius: 8,
  boxSizing: "border-box",
  backgroundColor: "#16213e",
  color: "white",
  fontSize: 15,
  outline: "none",
});

const labelStyle: React.CSSProperties = {
  display: "block",
  marginBottom: 6,
  fontWeight: 600,
  fontSize: 14,
  color: "#ccc",
};

const errorStyle: React.CSSProperties = {
  color: "#e94560",
  fontSize: 12,
  margin: "4px 0 0",
};

const fieldGroupStyle: React.CSSProperties = {
  marginBottom: 16,
};

const submitBtnStyle = (disabled: boolean): React.CSSProperties => ({
  width: "100%",
  padding: 12,
  marginTop: 8,
  backgroundColor: disabled ? "#999" : "#e94560",
  color: "#fff",
  border: "none",
  borderRadius: 8,
  cursor: disabled ? "not-allowed" : "pointer",
  fontSize: 16,
  fontWeight: 600,
});

// ── Componente ───────────────────────────────────────────────────────────────

export function RegistroForm({ onSuccess }: RegistroFormProps) {
  const [name, setName] = useState("");
  const [alias, setAlias] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitError(null);

    // Validate ALL fields before submitting
    const errors: FieldErrors = {
      name: validateName(name),
      alias: validateAlias(alias),
      email: validateEmail(email),
      password: validatePassword(password),
    };
    setFieldErrors(errors);

    if (Object.values(errors).some(Boolean)) return;

    setIsSubmitting(true);

    try {
      await registrarParticipante({
        name: name.trim(),
        alias: alias.trim(),
        email: email.trim(),
        password,
      });
      onSuccess();
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 409) {
          setSubmitError("El alias o email ya están registrados.");
        } else {
          setSubmitError(err.body || "Error al registrar. Inténtalo de nuevo.");
        }
      } else {
        setSubmitError("Error de conexión. Verifica tu conexión a internet.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} noValidate style={{ maxWidth: 400, margin: "0 auto" }}>
      {/* Error general */}
      {submitError && (
        <div
          style={{
            color: "#dc3545",
            marginBottom: 16,
            padding: "8px 12px",
            border: "1px solid #dc3545",
            borderRadius: 4,
            backgroundColor: "#2d1a1a",
          }}
        >
          {submitError}
        </div>
      )}

      {/* Nombre */}
      <div style={fieldGroupStyle}>
        <label htmlFor="reg-name" style={labelStyle}>
          Nombre
        </label>
        <input
          id="reg-name"
          type="text"
          value={name}
          onChange={(e) => setName(e.target.value)}
          style={inputStyle(Boolean(fieldErrors.name))}
          autoComplete="name"
        />
        {fieldErrors.name && <p style={errorStyle}>{fieldErrors.name}</p>}
      </div>

      {/* Alias */}
      <div style={fieldGroupStyle}>
        <label htmlFor="reg-alias" style={labelStyle}>
          Alias
        </label>
        <input
          id="reg-alias"
          type="text"
          value={alias}
          onChange={(e) => setAlias(e.target.value)}
          style={inputStyle(Boolean(fieldErrors.alias))}
          autoComplete="username"
        />
        {fieldErrors.alias && <p style={errorStyle}>{fieldErrors.alias}</p>}
      </div>

      {/* Email */}
      <div style={fieldGroupStyle}>
        <label htmlFor="reg-email" style={labelStyle}>
          Email
        </label>
        <input
          id="reg-email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          style={inputStyle(Boolean(fieldErrors.email))}
          autoComplete="email"
        />
        {fieldErrors.email && <p style={errorStyle}>{fieldErrors.email}</p>}
      </div>

      {/* Contraseña */}
      <div style={{ ...fieldGroupStyle, marginBottom: 20 }}>
        <label htmlFor="reg-password" style={labelStyle}>
          Contraseña
        </label>
        <input
          id="reg-password"
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

      {/* Submit */}
      <button type="submit" disabled={isSubmitting} style={submitBtnStyle(isSubmitting)}>
        {isSubmitting ? "Registrando..." : "Registrarse"}
      </button>
    </form>
  );
}
