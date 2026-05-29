import { useGame } from "../../../contexts/GameContext";

export function WaitingRoom() {
  const { state } = useGame();

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
      <div style={{
        width: 64,
        height: 64,
        border: "4px solid #0f3460",
        borderTopColor: "#e94560",
        borderRadius: "50%",
        animation: "spin 1s linear infinite",
        marginBottom: "1.5rem",
      }} />
      <style>{`
        @keyframes spin {
          to { transform: rotate(360deg); }
        }
      `}</style>

      <h2 style={{ color: "#e94560", marginBottom: "0.5rem" }}>
        {state.sessionName || "Sesión de Juego"}
      </h2>
      <p style={{ color: "#999" }}>
        Esperando al host para comenzar la experiencia...
      </p>
    </div>
  );
}
