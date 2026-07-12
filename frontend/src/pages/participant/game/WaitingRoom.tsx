import { useGame } from "../../../contexts/GameContext";

export function WaitingRoom({ loading = false }: { loading?: boolean }) {
  const { state } = useGame();

  return (
    <div className="waiting-room">
      <div className="spinner" />
      <h2>{state.sessionName || "Sesión de Juego"}</h2>
      <p>
        {loading
          ? "Conectando a la sesión..."
          : "Esperando al host para comenzar la experiencia..."}
      </p>
    </div>
  );
}
