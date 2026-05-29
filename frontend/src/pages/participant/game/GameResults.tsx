import { useNavigate } from "react-router-dom";
import { useGame } from "../../../contexts/GameContext";

export function GameResults() {
  const { state } = useGame();
  const navigate = useNavigate();

  const isFinished = state.sessionStatus === "Finished";

  function handleReturn() {
    if (state.sessionId) {
      sessionStorage.removeItem(`joined_${state.sessionId}`);
    }
    navigate("/", { replace: true });
  }

  return (
    <div style={{
      display: "flex",
      flexDirection: "column",
      alignItems: "center",
      justifyContent: "center",
      minHeight: "100vh",
      padding: "2rem",
      textAlign: "center",
    }}>
      {isFinished ? (
        <>
          <div style={{ fontSize: "3rem", marginBottom: "1rem" }}>🏆</div>
          <h2 style={{ color: "#e94560", marginBottom: "0.5rem" }}>¡Juego Terminado!</h2>
          <p style={{ color: "#999" }}>La experiencia ha finalizado.</p>
        </>
      ) : (
        <>
          <div style={{ fontSize: "3rem", marginBottom: "1rem" }}>⚠️</div>
          <h2 style={{ color: "#ffc107", marginBottom: "0.5rem" }}>Sesión Cancelada</h2>
          <p style={{ color: "#999" }}>La sesión fue cancelada por el host.</p>
        </>
      )}

      <div style={{
        marginTop: "2rem",
        padding: "1.5rem 2rem",
        backgroundColor: "#16213e",
        borderRadius: 12,
        border: "2px solid #e94560",
      }}>
        <div style={{ color: "#999", fontSize: "0.9rem", marginBottom: 4 }}>
          Puntaje Final
        </div>
        <div style={{
          fontSize: "2.5rem",
          fontWeight: "bold",
          color: "#e94560",
        }}>
          {state.score}
        </div>
      </div>

      <button
        onClick={handleReturn}
        style={{
          marginTop: "2rem",
          padding: "12px 32px",
          backgroundColor: "#0f3460",
          color: "white",
          border: "none",
          borderRadius: 8,
          cursor: "pointer",
          fontSize: "1rem",
          fontWeight: 600,
        }}
      >
        Volver al inicio
      </button>
    </div>
  );
}
