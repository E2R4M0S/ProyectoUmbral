import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { joinSession, ApiError } from "../../services/sessionsApi";
import { getServerHost, isProductionApk } from "../../config/serverConfig";

function validatePin(value: string): string | undefined {
  if (!value.trim()) return "El PIN es obligatorio.";
  if (value.trim().length !== 6) return "El PIN debe tener 6 caracteres.";
  return undefined;
}

export function UnirseSesion() {
  const navigate = useNavigate();
  const [pin, setPin] = useState("");
  const [pinError, setPinError] = useState<string>();
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitError(null);

    const error = validatePin(pin);
    if (error) { setPinError(error); return; }
    setPinError(undefined);

    setIsSubmitting(true);
    try {
      const result = await joinSession(pin.trim().toUpperCase());
      sessionStorage.setItem(`joined_${result.sessionId}`, "1");
      sessionStorage.setItem(`pin_${result.sessionId}`, pin.trim().toUpperCase());
      navigate(`/juego/${result.sessionId}`);
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 404) setSubmitError("PIN inválido. Verifica el código.");
        else setSubmitError("Error al unirse. Inténtalo de nuevo.");
      } else {
        const host = isProductionApk ? (getServerHost() || "no configurado") : "localhost";
        const detail = (err as Error).message || String(err);
        setSubmitError(`Error (${host}:5000): ${detail}`);
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="code-entry-page">
      <h2>Unirse a Sesión</h2>
      <p className="page-subtitle">
        Ingresa el PIN que te compartió el host para unirte a la experiencia
      </p>

      {submitError && <div className="alert alert-error" style={{ marginBottom: "1rem" }}>{submitError}</div>}

      <form onSubmit={handleSubmit}>
        <label className="form-label" htmlFor="pin">Código PIN</label>
        <input
          id="pin"
          type="text"
          maxLength={6}
          placeholder="ABC123"
          value={pin}
          onChange={(e) => setPin(e.target.value.toUpperCase())}
          className={`code-input-field${pinError ? " has-error" : ""}`}
          autoFocus
        />
        {pinError && <p className="code-field-error">{pinError}</p>}

        <button
          type="submit"
          disabled={isSubmitting}
          className="btn btn-primary"
          style={{ width: "100%", marginTop: "1rem" }}
        >
          {isSubmitting ? "Uniéndose..." : "Entrar a la Sesión"}
        </button>
      </form>
    </div>
  );
}
