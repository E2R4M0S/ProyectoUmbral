import { useGame } from "../../../contexts/GameContext";
import { Timer } from "../../../components/game/Timer";
import { ClueCard } from "../../../components/game/ClueCard";
import { RankingBoard } from "../../../components/game/RankingBoard";
import { QuestionCard } from "../../../components/game/QuestionCard";

export function ActiveGame() {
  const { state } = useGame();

  return (
    <div className="active-game">
      <Timer />
      <RankingBoard ranking={state.ranking} />

      {state.currentQuestion && (
        <QuestionCard key={state.currentQuestion.questionId} question={state.currentQuestion} />
      )}

      {!state.currentQuestion && state.clues.length === 0 ? (
        <div className="waiting-msg">
          {state.sessionStatus === "Active" ? "Esperando contenido..." : "Aún no hay pistas disponibles. ¡Prestá atención!"}
        </div>
      ) : !state.currentQuestion && state.clues.length > 0 ? (
        <div style={{ marginTop: "1rem" }}>
          {state.clues.map((clue: unknown, index: number) => (
            <ClueCard key={index} clue={clue} />
          ))}
        </div>
      ) : null}
    </div>
  );
}
