import { useState, useEffect } from "react";
import { useAuth } from "../../auth/useAuth";
import { getProfile, updateProfile, ApiError } from "../../services/perfilApi";
import type { PerfilData, UpdatePerfilRequest } from "../../types/perfil";

type FormState = "idle" | "loading" | "saving";

interface ValidationErrors {
  name?: string;
  alias?: string;
  general?: string;
}

function validate(data: UpdatePerfilRequest): ValidationErrors {
  const e: ValidationErrors = {};
  if (!data.name.trim())                     e.name  = "El nombre es requerido.";
  else if (data.name.trim().length > 100)    e.name  = "El nombre no puede superar los 100 caracteres.";
  if (!data.alias.trim())                    e.alias = "El alias es requerido.";
  else if (data.alias.trim().length < 3)     e.alias = "El alias debe tener al menos 3 caracteres.";
  else if (data.alias.trim().length > 50)    e.alias = "El alias no puede superar los 50 caracteres.";
  else if (!/^[a-zA-Z0-9_]+$/.test(data.alias.trim()))
    e.alias = "El alias solo puede contener letras, números y guiones bajos.";
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

  const [name, setName]   = useState("");
  const [alias, setAlias] = useState("");

  useEffect(() => { loadProfile(); }, []);

  async function loadProfile() {
    setFormState("loading");
    setLoading(true);
    setErrors({});
    try {
      const data = await getProfile();
      setProfile(data);
      setName(data.name);
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

    const formData: UpdatePerfilRequest = { name: name.trim(), alias: alias.trim() };
    const ve = validate(formData);
    if (Object.keys(ve).length > 0) { setErrors(ve); return; }

    setFormState("saving");
    setErrors({});

    try {
      const updated = await updateProfile(formData);
      setProfile(updated);
      setName(updated.name);
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
                if (f === "name")  fe.name  = d.message;
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

  if (loading) {
    return (
      <div className="page" style={{ textAlign: "center", paddingTop: "4rem" }}>
        <div className="spinner" style={{ margin: "0 auto" }} />
      </div>
    );
  }

  const isBusy = formState === "saving" || formState === "loading";

  return (
    <div className="page" style={{ maxWidth: 480 }}>
      <div className="page-header">
        <h1 className="page-title">Mi Perfil</h1>
      </div>

      {profile && (
        <div className="card profile-avatar-card">
          <div className="profile-avatar">
            {(profile.name || "?")[0].toUpperCase()}
          </div>
          <div>
            <div className="profile-name">{profile.name}</div>
            <div className="profile-alias">@{profile.alias}</div>
          </div>
        </div>
      )}

      {successMsg && <div className="alert alert-success" style={{ marginBottom: "1.25rem" }}>{successMsg}</div>}
      {errors.general && !errors.name && !errors.alias && (
        <div className="alert alert-error" style={{ marginBottom: "1.25rem" }}>{errors.general}</div>
      )}

      <div className="card">
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label className="form-label" htmlFor="name">Nombre</label>
            <input
              id="name"
              type="text"
              className={`form-input${errors.name ? " input-error" : ""}`}
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Tu nombre"
              disabled={isBusy}
            />
            {errors.name && <span className="form-hint form-hint-error">{errors.name}</span>}
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="alias">Alias</label>
            <input
              id="alias"
              type="text"
              className={`form-input${errors.alias ? " input-error" : ""}`}
              value={alias}
              onChange={(e) => setAlias(e.target.value)}
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
            <button
              type="button"
              className="btn btn-secondary"
              onClick={loadProfile}
              disabled={isBusy}
            >
              Descartar
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
