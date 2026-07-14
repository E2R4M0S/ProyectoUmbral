import { useState, type FormEvent } from "react";
import { startSession, ApiError } from "../../services/sessionsApi";

interface IniciarJuegoProps {
  sessionId: string;
  sessionName: string;
  onGameStarted?: () => void;
}

export function IniciarJuego({ sessionId, sessionName, onGameStarted }: IniciarJuegoProps) {
  const [isStarting, setIsStarting]   = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [success, setSuccess]         = useState(false);

  async function handleStartGame(e: FormEvent) {
    e.preventDefault();
    setSubmitError(null);
    setSuccess(false);
    setIsStarting(true);

    try {
      await startSession(sessionId);
      setSuccess(true);
      onGameStarted?.();
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 404)      setSubmitError("Sesión no encontrada.");
        else if (err.status === 400) setSubmitError("No se puede iniciar la sesión en su estado actual.");
        else                         setSubmitError("Error al iniciar la partida. Inténtalo de nuevo.");
      } else {
        setSubmitError("Error de conexión. Verifica tu conexión a internet.");
      }
    } finally {
      setIsStarting(false);
    }
  }

  return (
    <div style={{ maxWidth: 400, margin: "0 auto" }}>
      <h3 style={{ marginBottom: "0.5rem" }}>Iniciar Partida</h3>
      <p style={{ marginBottom: "1.25rem", color: "var(--text-muted)", fontSize: "0.875rem" }}>
        Sesión: <strong style={{ color: "var(--text-primary)" }}>{sessionName}</strong>
      </p>

      {success && (
        <div className="alert alert-success" style={{ marginBottom: "1rem" }}>
          ¡Partida iniciada correctamente!
        </div>
      )}
      {submitError && (
        <div className="alert alert-error" style={{ marginBottom: "1rem" }}>
          {submitError}
        </div>
      )}

      <form onSubmit={handleStartGame}>
        <button type="submit" className="btn btn-success" style={{ width: "100%" }} disabled={isStarting}>
          {isStarting ? "Iniciando..." : "▶ Iniciar Partida"}
        </button>
      </form>
    </div>
  );
}
