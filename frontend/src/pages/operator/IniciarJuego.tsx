import { useState, type FormEvent } from "react";
import { startSession, ApiError } from "../../services/sessionsApi";

interface IniciarJuegoProps {
  sessionId: string;
  sessionName: string;
  onGameStarted?: () => void;
}

const containerStyle: React.CSSProperties = {
  maxWidth: 400,
  margin: "0 auto",
  padding: "1rem",
};

const buttonStyle = (disabled: boolean): React.CSSProperties => ({
  width: "100%",
  padding: 12,
  backgroundColor: disabled ? "#999" : "#28a745",
  color: "#fff",
  border: "none",
  borderRadius: 4,
  cursor: disabled ? "not-allowed" : "pointer",
  fontSize: 16,
  fontWeight: 600,
});

export function IniciarJuego({ sessionId, sessionName, onGameStarted }: IniciarJuegoProps) {
  const [isStarting, setIsStarting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

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
        if (err.status === 404) {
          setSubmitError("Sesión no encontrada.");
        } else if (err.status === 400) {
          setSubmitError("No se puede iniciar la sesión en su estado actual.");
        } else {
          setSubmitError("Error al iniciar la partida. Intentalo de nuevo.");
        }
      } else {
        setSubmitError("Error de conexión. Verificá tu conexión a internet.");
      }
    } finally {
      setIsStarting(false);
    }
  }

  return (
    <div style={containerStyle}>
      <h2 style={{ marginBottom: "1rem" }}>Iniciar Partida</h2>
      <p style={{ marginBottom: "1rem", color: "#555" }}>
        Sesión: <strong>{sessionName}</strong>
      </p>

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
          ¡Partida iniciada correctamente!
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

      <form onSubmit={handleStartGame}>
        <button type="submit" disabled={isStarting} style={buttonStyle(isStarting)}>
          {isStarting ? "Iniciando..." : "Iniciar Partida"}
        </button>
      </form>
    </div>
  );
}
