import { useGame } from "../../../contexts/GameContext";
import { Timer } from "../../../components/game/Timer";
import { ClueCard } from "../../../components/game/ClueCard";

export function ActiveGame() {
  const { state } = useGame();

  return (
    <div style={{
      minHeight: "100vh",
      padding: "1rem",
      maxWidth: 600,
      margin: "0 auto",
    }}>
      <Timer />

      {state.clues.length === 0 ? (
        <div style={{ textAlign: "center", color: "#999", marginTop: "2rem" }}>
          Aún no hay pistas disponibles. ¡Prestá atención!
        </div>
      ) : (
        <div style={{ marginTop: "1rem" }}>
          {state.clues.map((clue: unknown, index: number) => (
            <ClueCard key={index} clue={clue} />
          ))}
        </div>
      )}
    </div>
  );
}
