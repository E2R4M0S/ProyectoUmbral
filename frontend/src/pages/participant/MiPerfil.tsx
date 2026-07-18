import { useState, useEffect } from "react";
import { useAuth } from "../../auth/useAuth";
import { getProfile, updateProfile, changePassword, ApiError } from "../../services/perfilApi";
import type { PerfilData, UpdatePerfilRequest } from "../../types/perfil";

type FormState = "idle" | "loading" | "saving";
type PasswordFormState = "idle" | "saving";

interface ValidationErrors {
  firstName?: string;
  lastName?: string;
  alias?: string;
  general?: string;
}

interface PasswordErrors {
  currentPassword?: string;
  newPassword?: string;
  confirmPassword?: string;
  general?: string;
}

function validate(data: UpdatePerfilRequest): ValidationErrors {
  const e: ValidationErrors = {};
  if (!data.firstName.trim())                  e.firstName = "El nombre es requerido.";
  else if (data.firstName.trim().length > 50)  e.firstName = "Máximo 50 caracteres.";
  if (!data.lastName.trim())                   e.lastName = "El apellido es requerido.";
  else if (data.lastName.trim().length > 50)   e.lastName = "Máximo 50 caracteres.";
  if (!data.alias.trim())                      e.alias = "El alias es requerido.";
  else if (data.alias.trim().length < 3)       e.alias = "Mínimo 3 caracteres.";
  else if (data.alias.trim().length > 50)      e.alias = "Máximo 50 caracteres.";
  else if (!/^[a-zA-Z0-9_]+$/.test(data.alias.trim()))
    e.alias = "Solo letras, números y guiones bajos.";
  return e;
}

function validatePassword(
  current: string,
  next: string,
  confirm: string,
): PasswordErrors {
  const e: PasswordErrors = {};
  if (!current)           e.currentPassword = "Ingresá tu contraseña actual.";
  if (!next)              e.newPassword = "Ingresá la nueva contraseña.";
  else if (next.length < 6) e.newPassword = "Mínimo 6 caracteres.";
  if (!confirm)           e.confirmPassword = "Confirmá la nueva contraseña.";
  else if (next && confirm !== next)
    e.confirmPassword = "Las contraseñas no coinciden.";
  return e;
}

export function MiPerfil() {
  const auth = useAuth();

  const [formState, setFormState]   = useState<FormState>("idle");
  const [profile, setProfile]       = useState<PerfilData | null>(null);
  const [loading, setLoading]       = useState(true);
  const [errors, setErrors]         = useState<ValidationErrors>({});
  const [successMsg, setSuccess]    = useState("");
  const [originalAlias, setOrigAlias] = useState("");

  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName]   = useState("");
  const [alias, setAlias]         = useState("");

  const [pwdState, setPwdState]       = useState<PasswordFormState>("idle");
  const [pwdErrors, setPwdErrors]     = useState<PasswordErrors>({});
  const [pwdSuccess, setPwdSuccess]   = useState("");
  const [currentPwd, setCurrentPwd]   = useState("");
  const [newPwd, setNewPwd]           = useState("");
  const [confirmPwd, setConfirmPwd]   = useState("");

  useEffect(() => { loadProfile(); }, []);

  async function loadProfile() {
    setFormState("loading");
    setLoading(true);
    setErrors({});
    try {
      const data = await getProfile();
      setProfile(data);
      setFirstName(data.firstName);
      setLastName(data.lastName);
      setAlias(data.alias);
      setOrigAlias(data.alias);
    } catch (err) {
      const msg = err instanceof ApiError
        ? `Error ${err.status}: ${err.body || "Sin respuesta"}`
        : err instanceof Error ? err.message : String(err);
      setErrors({ general: `No se pudo cargar el perfil. ${msg}` });
    } finally {
      setFormState("idle");
      setLoading(false);
    }
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSuccess("");

    const formData: UpdatePerfilRequest = { firstName: firstName.trim(), lastName: lastName.trim(), alias: alias.trim() };
    const ve = validate(formData);
    if (Object.keys(ve).length > 0) { setErrors(ve); return; }

    setFormState("saving");
    setErrors({});

    try {
      const updated = await updateProfile(formData);
      setProfile(updated);
      setFirstName(updated.firstName);
      setLastName(updated.lastName);
      setAlias(updated.alias);
      setOrigAlias(updated.alias);
      setSuccess("Perfil actualizado correctamente.");

      if (updated.alias !== originalAlias) {
        try { await auth.signinSilent(); } catch { /* best-effort */ }
      }
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 409) {
          setErrors({ alias: "Ese alias ya está en uso por otro participante. Elegí otro." });
        } else if (err.status === 400) {
          try {
            const parsed = JSON.parse(err.body);
            const fe: ValidationErrors = {};
            if (Array.isArray(parsed.details)) {
              for (const d of parsed.details) {
                const f = d.field?.toLowerCase();
                if (f === "firstname")  fe.firstName = d.message;
                else if (f === "lastname") fe.lastName = d.message;
                else if (f === "alias") fe.alias = d.message;
                else fe.general = d.message;
              }
            }
            setErrors(fe);
          } catch {
            setErrors({ general: "Error de validación. Revisá los datos." });
          }
        } else {
          setErrors({ general: "Error del servidor. Inténtalo de nuevo más tarde." });
        }
      } else {
        setErrors({ general: "Ocurrió un error inesperado. Inténtalo de nuevo." });
      }
    } finally {
      setFormState("idle");
    }
  }

  async function handleChangePassword(e: React.FormEvent) {
    e.preventDefault();
    setPwdSuccess("");

    const ve = validatePassword(currentPwd, newPwd, confirmPwd);
    if (Object.keys(ve).length > 0) { setPwdErrors(ve); return; }

    setPwdState("saving");
    setPwdErrors({});

    try {
      await changePassword(currentPwd, newPwd);
      setPwdSuccess("Contraseña actualizada correctamente.");
      setCurrentPwd("");
      setNewPwd("");
      setConfirmPwd("");
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 403) {
          setPwdErrors({ currentPassword: "La contraseña actual es incorrecta." });
        } else if (err.status === 400) {
          setPwdErrors({ newPassword: "La nueva contraseña no es válida." });
        } else {
          setPwdErrors({ general: "No se pudo cambiar la contraseña. Inténtalo de nuevo." });
        }
      } else {
        setPwdErrors({ general: "Ocurrió un error inesperado. Inténtalo de nuevo." });
      }
    } finally {
      setPwdState("idle");
    }
  }

  if (loading) {
    return (
      <div className="page" style={{ textAlign: "center", paddingTop: "4rem" }}>
        <div className="spinner" style={{ margin: "0 auto" }} />
      </div>
    );
  }

  const isBusy = formState === "saving" || formState === "loading";
  const isPwdBusy = pwdState === "saving";
  const fullName = `${profile?.firstName ?? ""} ${profile?.lastName ?? ""}`.trim();

  return (
    <div className="page" style={{ maxWidth: 480 }}>
      <div className="page-header">
        <h1 className="page-title">Mi Perfil</h1>
      </div>

      {profile && (
        <div className="card" style={{ marginBottom: "1.5rem", display: "flex", alignItems: "center", gap: "1rem" }}>
          <div style={{
            width: 52, height: 52, borderRadius: "50%",
            background: "var(--accent)", display: "flex", alignItems: "center",
            justifyContent: "center", fontWeight: 800, fontSize: "1.25rem", flexShrink: 0,
          }}>
            {(profile.firstName || "?")[0].toUpperCase()}
          </div>
          <div>
            <div style={{ fontWeight: 700 }}>{fullName}</div>
            <div style={{ color: "var(--text-muted)", fontSize: "0.875rem" }}>@{profile.alias}</div>
          </div>
        </div>
      )}

      {successMsg && <div className="alert alert-success" style={{ marginBottom: "1.25rem" }}>{successMsg}</div>}
      {errors.general && !errors.firstName && !errors.lastName && !errors.alias && (
        <div className="alert alert-error" style={{ marginBottom: "1.25rem" }}>{errors.general}</div>
      )}

      <div className="card">
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label className="form-label" htmlFor="firstName">Nombre</label>
            <input
              id="firstName" type="text" className="form-input"
              style={errors.firstName ? { borderColor: "var(--color-error)" } : {}}
              value={firstName} onChange={(e) => setFirstName(e.target.value)}
              placeholder="Tu nombre" disabled={isBusy}
            />
            {errors.firstName && <span className="form-hint" style={{ color: "var(--color-error)" }}>{errors.firstName}</span>}
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="lastName">Apellido</label>
            <input
              id="lastName" type="text" className="form-input"
              style={errors.lastName ? { borderColor: "var(--color-error)" } : {}}
              value={lastName} onChange={(e) => setLastName(e.target.value)}
              placeholder="Tu apellido" disabled={isBusy}
            />
            {errors.lastName && <span className="form-hint" style={{ color: "var(--color-error)" }}>{errors.lastName}</span>}
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="alias">Alias</label>
            <input
              id="alias" type="text" className="form-input"
              style={errors.alias ? { borderColor: "var(--color-error)" } : {}}
              value={alias} onChange={(e) => setAlias(e.target.value)}
              placeholder="solo letras, números y _"
              disabled={isBusy}
            />
            {errors.alias
              ? <span className="form-hint form-hint-error">{errors.alias}</span>
              : <span className="form-hint">Solo letras, números y guiones bajos.</span>
            }
          </div>

          <div className="form-actions">
            <button type="submit" className="btn btn-primary" disabled={isBusy} style={{ flex: 1 }}>
              {formState === "saving" ? "Guardando..." : "Guardar cambios"}
            </button>
            <button type="button" className="btn btn-secondary" onClick={loadProfile} disabled={isBusy}>
              Descartar
            </button>
          </div>
        </form>
      </div>

      <div className="page-header" style={{ marginTop: "2rem" }}>
        <h2 className="page-title" style={{ fontSize: "1.25rem" }}>Cambiar contraseña</h2>
      </div>

      {pwdSuccess && <div className="alert alert-success" style={{ marginBottom: "1.25rem" }}>{pwdSuccess}</div>}
      {pwdErrors.general && !pwdErrors.currentPassword && !pwdErrors.newPassword && !pwdErrors.confirmPassword && (
        <div className="alert alert-error" style={{ marginBottom: "1.25rem" }}>{pwdErrors.general}</div>
      )}

      <div className="card">
        <form onSubmit={handleChangePassword}>
          <div className="form-group">
            <label className="form-label" htmlFor="currentPassword">Contraseña actual</label>
            <input
              id="currentPassword" type="password" className="form-input"
              style={pwdErrors.currentPassword ? { borderColor: "var(--color-error)" } : {}}
              value={currentPwd} onChange={(e) => setCurrentPwd(e.target.value)}
              placeholder="Tu contraseña actual" disabled={isPwdBusy}
              autoComplete="current-password"
            />
            {pwdErrors.currentPassword && <span className="form-hint form-hint-error">{pwdErrors.currentPassword}</span>}
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="newPassword">Nueva contraseña</label>
            <input
              id="newPassword" type="password" className="form-input"
              style={pwdErrors.newPassword ? { borderColor: "var(--color-error)" } : {}}
              value={newPwd} onChange={(e) => setNewPwd(e.target.value)}
              placeholder="Mínimo 6 caracteres" disabled={isPwdBusy}
              autoComplete="new-password"
            />
            {pwdErrors.newPassword
              ? <span className="form-hint form-hint-error">{pwdErrors.newPassword}</span>
              : <span className="form-hint">Mínimo 6 caracteres.</span>
            }
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="confirmPassword">Confirmar nueva contraseña</label>
            <input
              id="confirmPassword" type="password" className="form-input"
              style={pwdErrors.confirmPassword ? { borderColor: "var(--color-error)" } : {}}
              value={confirmPwd} onChange={(e) => setConfirmPwd(e.target.value)}
              placeholder="Repetí la nueva contraseña" disabled={isPwdBusy}
              autoComplete="new-password"
            />
            {pwdErrors.confirmPassword && <span className="form-hint form-hint-error">{pwdErrors.confirmPassword}</span>}
          </div>

          <div className="form-actions">
            <button type="submit" className="btn btn-primary" disabled={isPwdBusy} style={{ flex: 1 }}>
              {pwdState === "saving" ? "Cambiando..." : "Cambiar contraseña"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
