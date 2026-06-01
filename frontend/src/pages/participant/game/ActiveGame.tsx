import { useGame } from "../../../contexts/GameContext";
import { Timer } from "../../../components/game/Timer";
import { ClueCard } from "../../../components/game/ClueCard";
import { CountdownTimer } from "../../../components/game/CountdownTimer";

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
      {/* Example: if the current clue contains a TimeLimitSeconds property, show countdown */}
      {state.clues.length > 0 && typeof state.clues[0] === "object" && (state.clues[0] as any).timeLimitSeconds && (
        <CountdownTimer timeLimitSeconds={(state.clues[0] as any).timeLimitSeconds} onExpired={() => {
          // when expired, dispatch an action to disable answers
          dispatch({ type: "SET_SCORE", score: state.score });
        }} />
      )}

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
