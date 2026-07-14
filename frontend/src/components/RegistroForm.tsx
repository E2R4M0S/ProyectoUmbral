import { useState, type FormEvent } from "react";
import { registrarParticipante, ApiError } from "../services/registroApi";

interface RegistroFormProps {
  onSuccess: () => void;
}

interface FieldErrors {
  firstName?: string;
  lastName?: string;
  username?: string;
  alias?: string;
  email?: string;
  password?: string;
}

function validateFirstName(v: string) {
  if (!v.trim()) return "El nombre es obligatorio.";
  if (v.trim().length > 50) return "Máximo 50 caracteres.";
}

function validateLastName(v: string) {
  if (!v.trim()) return "El apellido es obligatorio.";
  if (v.trim().length > 50) return "Máximo 50 caracteres.";
}

function validateUsername(v: string) {
  if (!v.trim()) return "El usuario es obligatorio.";
  if (v.trim().length < 3) return "Mínimo 3 caracteres.";
  if (v.trim().length > 30) return "Máximo 30 caracteres.";
  if (!/^[a-zA-Z0-9_]+$/.test(v.trim()))
    return "Solo letras, números y guiones bajos.";
}

function validateAlias(v: string) {
  if (!v.trim()) return "El alias es obligatorio.";
  if (v.trim().length < 3) return "Mínimo 3 caracteres.";
  if (v.trim().length > 50) return "Máximo 50 caracteres.";
  if (!/^[a-zA-Z0-9_]+$/.test(v.trim()))
    return "Solo letras, números y guiones bajos.";
}

function validateEmail(v: string) {
  if (!v.trim()) return "El email es obligatorio.";
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v.trim()))
    return "Formato de email inválido.";
}

function validatePassword(v: string) {
  if (!v) return "La contraseña es obligatoria.";
  if (v.length < 8) return "Mínimo 8 caracteres.";
}

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

const rowStyle: React.CSSProperties = {
  display: "flex",
  gap: 12,
};

const halfFieldStyle: React.CSSProperties = {
  flex: 1,
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

export function RegistroForm({ onSuccess }: RegistroFormProps) {
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [username, setUsername] = useState("");
  const [alias, setAlias] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitError(null);

    const errors: FieldErrors = {
      firstName: validateFirstName(firstName),
      lastName: validateLastName(lastName),
      username: validateUsername(username),
      alias: validateAlias(alias),
      email: validateEmail(email),
      password: validatePassword(password),
    };
    setFieldErrors(errors);
    if (Object.values(errors).some(Boolean)) return;

    setIsSubmitting(true);
    try {
      await registrarParticipante({
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        username: username.trim(),
        alias: alias.trim(),
        email: email.trim(),
        password,
      });
      onSuccess();
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 409) {
          setSubmitError("El usuario, alias o email ya están registrados.");
        } else {
          setSubmitError(err.body || "Error al registrar. Intentalo de nuevo.");
        }
      } else {
        setSubmitError("Error de conexión. Verificá tu conexión a internet.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} noValidate style={{ maxWidth: 400, margin: "0 auto" }}>
      {submitError && (
        <div style={{ color: "#dc3545", marginBottom: 16, padding: "8px 12px", border: "1px solid #dc3545", borderRadius: 4, backgroundColor: "#2d1a1a" }}>
          {submitError}
        </div>
      )}

      <div style={rowStyle}>
        <div style={halfFieldStyle}>
          <label htmlFor="reg-firstName" style={labelStyle}>Nombre</label>
          <input id="reg-firstName" type="text" value={firstName}
            onChange={(e) => setFirstName(e.target.value)}
            style={inputStyle(Boolean(fieldErrors.firstName))} autoComplete="given-name" />
          {fieldErrors.firstName && <p style={errorStyle}>{fieldErrors.firstName}</p>}
        </div>
        <div style={halfFieldStyle}>
          <label htmlFor="reg-lastName" style={labelStyle}>Apellido</label>
          <input id="reg-lastName" type="text" value={lastName}
            onChange={(e) => setLastName(e.target.value)}
            style={inputStyle(Boolean(fieldErrors.lastName))} autoComplete="family-name" />
          {fieldErrors.lastName && <p style={errorStyle}>{fieldErrors.lastName}</p>}
        </div>
      </div>

      <div style={fieldGroupStyle}>
        <label htmlFor="reg-username" style={labelStyle}>Nombre de usuario</label>
        <input id="reg-username" type="text" value={username}
          onChange={(e) => setUsername(e.target.value)}
          style={inputStyle(Boolean(fieldErrors.username))} autoComplete="username" />
        {fieldErrors.username && <p style={errorStyle}>{fieldErrors.username}</p>}
      </div>

      <div style={fieldGroupStyle}>
        <label htmlFor="reg-alias" style={labelStyle}>Alias</label>
        <input id="reg-alias" type="text" value={alias}
          onChange={(e) => setAlias(e.target.value)}
          style={inputStyle(Boolean(fieldErrors.alias))} />
        {fieldErrors.alias && <p style={errorStyle}>{fieldErrors.alias}</p>}
      </div>

      <div style={fieldGroupStyle}>
        <label htmlFor="reg-email" style={labelStyle}>Email</label>
        <input id="reg-email" type="email" value={email}
          onChange={(e) => setEmail(e.target.value)}
          style={inputStyle(Boolean(fieldErrors.email))} autoComplete="email" />
        {fieldErrors.email && <p style={errorStyle}>{fieldErrors.email}</p>}
      </div>

      <div style={{ ...fieldGroupStyle, marginBottom: 20 }}>
        <label htmlFor="reg-password" style={labelStyle}>Contraseña</label>
        <input id="reg-password" type="password" value={password}
          onChange={(e) => setPassword(e.target.value)}
          style={inputStyle(Boolean(fieldErrors.password))} autoComplete="new-password" />
        {fieldErrors.password && <p style={errorStyle}>{fieldErrors.password}</p>}
      </div>

      <button type="submit" disabled={isSubmitting} style={submitBtnStyle(isSubmitting)}>
        {isSubmitting ? "Registrando..." : "Registrarse"}
      </button>
    </form>
  );
}
