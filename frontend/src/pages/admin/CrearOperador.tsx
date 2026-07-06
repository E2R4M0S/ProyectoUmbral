import { useState, type FormEvent } from "react";
import { crearOperador, ApiError } from "../../services/operadorApi";

interface FieldErrors {
  name?: string;
  email?: string;
  password?: string;
}

function validateName(v: string)     { if (!v.trim()) return "El nombre es obligatorio."; if (v.trim().length > 100) return "Máximo 100 caracteres."; }
function validateEmail(v: string)    { if (!v.trim()) return "El email es obligatorio."; if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v.trim())) return "Formato de email inválido."; }
function validatePassword(v: string) { if (!v) return "La contraseña es obligatoria."; }

export function CrearOperador() {
  const [name, setName]         = useState("");
  const [email, setEmail]       = useState("");
  const [password, setPass]     = useState("");
  const [errors, setErrors]     = useState<FieldErrors>({});
  const [submitErr, setSubmitErr] = useState<string | null>(null);
  const [submitting, setSub]    = useState(false);
  const [success, setSuccess]   = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitErr(null);
    setSuccess(false);

    const fe: FieldErrors = {
      name:     validateName(name),
      email:    validateEmail(email),
      password: validatePassword(password),
    };
    setErrors(fe);
    if (Object.values(fe).some(Boolean)) return;

    setSub(true);
    try {
      await crearOperador({ name: name.trim(), email: email.trim(), password });
      setSuccess(true);
      setName(""); setEmail(""); setPass("");
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 409)      setSubmitErr("El email ya está registrado.");
        else if (err.status === 400) setSubmitErr("Datos inválidos. Verificá los campos.");
        else                         setSubmitErr("Error al crear el operador. Intentalo de nuevo.");
      } else {
        setSubmitErr("Error de conexión. Verificá tu conexión a internet.");
      }
    } finally {
      setSub(false);
    }
  }

  return (
    <div className="page" style={{ maxWidth: 480 }}>
      <div className="page-header">
        <h1 className="page-title">Crear Operador</h1>
      </div>

      <div className="card">
        {success && (
          <div className="alert alert-success" style={{ marginBottom: "1.25rem" }}>
            Operador creado correctamente.
          </div>
        )}
        {submitErr && (
          <div className="alert alert-error" style={{ marginBottom: "1.25rem" }}>
            {submitErr}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label className="form-label" htmlFor="op-name">Nombre</label>
            <input
              id="op-name"
              type="text"
              className="form-input"
              style={errors.name ? { borderColor: "var(--color-error)" } : {}}
              value={name}
              onChange={(e) => setName(e.target.value)}
              autoComplete="name"
            />
            {errors.name && <span className="form-hint" style={{ color: "var(--color-error)" }}>{errors.name}</span>}
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="op-email">Email</label>
            <input
              id="op-email"
              type="email"
              className="form-input"
              style={errors.email ? { borderColor: "var(--color-error)" } : {}}
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              autoComplete="email"
            />
            {errors.email && <span className="form-hint" style={{ color: "var(--color-error)" }}>{errors.email}</span>}
          </div>

          <div className="form-group" style={{ marginBottom: "1.5rem" }}>
            <label className="form-label" htmlFor="op-password">Contraseña</label>
            <input
              id="op-password"
              type="password"
              className="form-input"
              style={errors.password ? { borderColor: "var(--color-error)" } : {}}
              value={password}
              onChange={(e) => setPass(e.target.value)}
              autoComplete="new-password"
            />
            {errors.password && <span className="form-hint" style={{ color: "var(--color-error)" }}>{errors.password}</span>}
          </div>

          <button type="submit" className="btn btn-primary" style={{ width: "100%" }} disabled={submitting}>
            {submitting ? "Creando..." : "Crear Operador"}
          </button>
        </form>
      </div>
    </div>
  );
}
