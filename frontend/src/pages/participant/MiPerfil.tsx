import { useState, useEffect } from "react";
import { useAuth } from "../../auth/useAuth";
import { getProfile, updateProfile, ApiError } from "../../services/perfilApi";
import type { PerfilData, UpdatePerfilRequest } from "../../types/perfil";

type FormState = "idle" | "loading" | "saving" | "success";

interface ValidationErrors {
  name?: string;
  alias?: string;
  general?: string;
}

export function MiPerfil() {
  const auth = useAuth();

  const [formState, setFormState] = useState<FormState>("idle");
  const [profile, setProfile] = useState<PerfilData | null>(null);
  const [loading, setLoading] = useState(true);
  const [errors, setErrors] = useState<ValidationErrors>({});
  const [successMessage, setSuccessMessage] = useState("");

  const [name, setName] = useState("");
  const [alias, setAlias] = useState("");
  const [originalAlias, setOriginalAlias] = useState("");

  useEffect(() => {
    loadProfile();
  }, []);

  async function loadProfile() {
    setFormState("loading");
    setLoading(true);
    setErrors({});
    try {
      const data = await getProfile();
      setProfile(data);
      setName(data.name);
      setAlias(data.alias);
      setOriginalAlias(data.alias);
      setFormState("idle");
    } catch (err) {
      setErrors({ general: "No se pudo cargar el perfil. Intentalo de nuevo." });
      setFormState("idle");
    } finally {
      setLoading(false);
    }
  }

  function validate(data: UpdatePerfilRequest): ValidationErrors {
    const validationErrors: ValidationErrors = {};
    if (!data.name.trim()) {
      validationErrors.name = "El nombre es requerido.";
    } else if (data.name.trim().length > 100) {
      validationErrors.name = "El nombre no puede superar los 100 caracteres.";
    }
    if (!data.alias.trim()) {
      validationErrors.alias = "El alias es requerido.";
    } else if (data.alias.trim().length < 3) {
      validationErrors.alias = "El alias debe tener al menos 3 caracteres.";
    } else if (data.alias.trim().length > 50) {
      validationErrors.alias = "El alias no puede superar los 50 caracteres.";
    } else if (!/^[a-zA-Z0-9_]+$/.test(data.alias.trim())) {
      validationErrors.alias =
        "El alias solo puede contener letras, números y guiones bajos.";
    }
    return validationErrors;
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSuccessMessage("");

    const formData: UpdatePerfilRequest = {
      name: name.trim(),
      alias: alias.trim(),
    };

    const validationErrors = validate(formData);
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    setFormState("saving");
    setErrors({});

    try {
      const updated = await updateProfile(formData);
      setProfile(updated);
      setName(updated.name);
      setAlias(updated.alias);
      setOriginalAlias(updated.alias);
      setSuccessMessage("Perfil actualizado correctamente.");

      // Refrescar JWT si el alias cambió para que los claims se actualicen.
      if (updated.alias !== originalAlias) {
        try {
          await auth.signinSilent();
        } catch {
          // El refresh del token es "best-effort"; no rompe el flujo.
        }
      }
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 409) {
          // 409: alias ya tomado.
          setErrors({
            alias:
              "Ese alias ya está en uso por otro participante. Elegí otro.",
          });
        } else if (err.status === 400) {
          // Intentar parsear errores de validación del backend.
          try {
            const parsed = JSON.parse(err.body);
            const fieldErrors: ValidationErrors = {};
            if (Array.isArray(parsed.details)) {
              for (const detail of parsed.details) {
                const f = detail.field?.toLowerCase();
                if (f === "name") fieldErrors.name = detail.message;
                else if (f === "alias") fieldErrors.alias = detail.message;
                else fieldErrors.general = detail.message;
              }
            }
            setErrors(fieldErrors);
          } catch {
            setErrors({ general: "Error de validación. Revisá los datos." });
          }
        } else {
          setErrors({
            general: "Error del servidor. Intentalo de nuevo más tarde.",
          });
        }
      } else {
        setErrors({
          general: "Ocurrió un error inesperado. Intentalo de nuevo.",
        });
      }
    } finally {
      setFormState("idle");
    }
  }

  if (loading) {
    return (
      <div style={containerStyle}>
        <div style={spinnerStyle}>Cargando perfil...</div>
      </div>
    );
  }

  return (
    <div style={containerStyle}>
      <h1 style={titleStyle}>Mi Perfil</h1>

      {successMessage && (
        <div style={successBannerStyle}>{successMessage}</div>
      )}

      {errors.general && !errors.name && !errors.alias && (
        <div style={errorBannerStyle}>{errors.general}</div>
      )}

      <form onSubmit={handleSubmit} style={formStyle}>
        <div style={fieldGroupStyle}>
          <label htmlFor="name" style={labelStyle}>
            Nombre
          </label>
          <input
            id="name"
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            style={{
              ...inputStyle,
              borderColor: errors.name ? "#dc2626" : "#d1d5db",
            }}
            placeholder="Tu nombre"
            disabled={formState === "saving"}
          />
          {errors.name && <span style={fieldErrorStyle}>{errors.name}</span>}
        </div>

        <div style={fieldGroupStyle}>
          <label htmlFor="alias" style={labelStyle}>
            Alias
          </label>
          <input
            id="alias"
            type="text"
            value={alias}
            onChange={(e) => setAlias(e.target.value)}
            style={{
              ...inputStyle,
              borderColor: errors.alias ? "#dc2626" : "#d1d5db",
            }}
            placeholder="Tu alias (solo letras, números y _)"
            disabled={formState === "saving"}
          />
          {errors.alias && (
            <span style={fieldErrorStyle}>{errors.alias}</span>
          )}
        </div>

        <div style={fieldGroupStyle}>
          <label htmlFor="email" style={labelStyle}>
            Correo electrónico
          </label>
          <input
            id="email"
            type="email"
            value={profile?.email ?? ""}
            readOnly
            style={{ ...inputStyle, backgroundColor: "#f3f4f6" }}
            placeholder="Tu correo"
          />
          <span style={hintStyle}>No editable</span>
        </div>

        <div style={buttonGroupStyle}>
          <button
            type="submit"
            disabled={formState === "saving" || formState === "loading"}
            style={{
              ...buttonStyle,
              opacity:
                formState === "saving" || formState === "loading" ? 0.6 : 1,
              cursor:
                formState === "saving" || formState === "loading"
                  ? "not-allowed"
                  : "pointer",
            }}
          >
            {formState === "saving" ? "Guardando..." : "Guardar cambios"}
          </button>

          <button
            type="button"
            onClick={loadProfile}
            disabled={formState === "saving"}
            style={{ ...buttonSecondaryStyle }}
          >
            Descartar cambios
          </button>
        </div>
      </form>
    </div>
  );
}

// --- Inline styles ---

const containerStyle: React.CSSProperties = {
  maxWidth: 480,
  margin: "0 auto",
  padding: "2rem 1rem",
};

const titleStyle: React.CSSProperties = {
  marginBottom: "1.5rem",
  fontSize: "1.5rem",
  fontWeight: 600,
};

const formStyle: React.CSSProperties = {
  display: "flex",
  flexDirection: "column",
  gap: "1.25rem",
};

const fieldGroupStyle: React.CSSProperties = {
  display: "flex",
  flexDirection: "column",
  gap: "0.375rem",
};

const labelStyle: React.CSSProperties = {
  fontSize: "0.875rem",
  fontWeight: 500,
  color: "#374151",
};

const inputStyle: React.CSSProperties = {
  padding: "0.5rem 0.75rem",
  borderWidth: 1,
  borderStyle: "solid",
  borderRadius: 6,
  fontSize: "0.9375rem",
  outline: "none",
  width: "100%",
  boxSizing: "border-box",
};

const fieldErrorStyle: React.CSSProperties = {
  fontSize: "0.8125rem",
  color: "#dc2626",
};

const hintStyle: React.CSSProperties = {
  fontSize: "0.8125rem",
  color: "#9ca3af",
};

const buttonGroupStyle: React.CSSProperties = {
  display: "flex",
  gap: "0.75rem",
  marginTop: "0.5rem",
};

const buttonStyle: React.CSSProperties = {
  padding: "0.625rem 1.25rem",
  backgroundColor: "#1d4ed8",
  color: "#ffffff",
  border: "none",
  borderRadius: 6,
  fontSize: "0.9375rem",
  fontWeight: 500,
  transition: "opacity 0.15s",
};

const buttonSecondaryStyle: React.CSSProperties = {
  padding: "0.625rem 1.25rem",
  backgroundColor: "#ffffff",
  color: "#374151",
  border: "1px solid #d1d5db",
  borderRadius: 6,
  fontSize: "0.9375rem",
  fontWeight: 500,
  cursor: "pointer",
};

const spinnerStyle: React.CSSProperties = {
  textAlign: "center",
  padding: "3rem 0",
  color: "#6b7280",
  fontSize: "0.9375rem",
};

const successBannerStyle: React.CSSProperties = {
  backgroundColor: "#dcfce7",
  border: "1px solid #86efac",
  borderRadius: 6,
  padding: "0.75rem 1rem",
  marginBottom: "1rem",
  color: "#166534",
  fontSize: "0.9375rem",
};

const errorBannerStyle: React.CSSProperties = {
  backgroundColor: "#fee2e2",
  border: "1px solid #fca5a5",
  borderRadius: 6,
  padding: "0.75rem 1rem",
  marginBottom: "1rem",
  color: "#991b1b",
  fontSize: "0.9375rem",
};
