import { useGame } from "../../../contexts/GameContext";

export function WaitingRoom() {
  const { state } = useGame();

  return (
    <div className="waiting-room">
      <div className="spinner" />
      <style>{`
        @keyframes spin {
          to { transform: rotate(360deg); }
        }
      `}</style>

      <h2>
        {state.sessionName || "Sesión de Juego"}
      </h2>
      <p>
        Esperando al host para comenzar la experiencia...
      </p>
    </div>
  );
}
