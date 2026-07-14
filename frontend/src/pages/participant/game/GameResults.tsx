import { useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useGame } from "../../../contexts/GameContext";
import { RankingBoard } from "../../../components/game/RankingBoard";

export function GameResults() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const { state, dispatch } = useGame();
  const navigate = useNavigate();

  const isFinished = state.sessionStatus === "Finished";

  useEffect(() => {
    if (sessionId) {
      const saved = sessionStorage.getItem(`score_${sessionId}`);
      if (saved) {
        dispatch({ type: "SET_SCORE", score: parseInt(saved, 10) });
      }
      const savedRanking = sessionStorage.getItem(`ranking_${sessionId}`);
      if (savedRanking) {
        dispatch({ type: "RANKING_UPDATED", ranking: JSON.parse(savedRanking) });
      }
    }
  }, [sessionId, dispatch]);

  function handleReturn() {
    if (state.sessionId) {
      sessionStorage.removeItem(`joined_${state.sessionId}`);
    }
    navigate("/", { replace: true });
  }

  return (
    <div className="game-results">
      {isFinished ? (
        <>
          <div className="results-icon">🏆</div>
          <h2 className="text-accent">¡Juego Terminado!</h2>
          <p>La experiencia ha finalizado.</p>

          <RankingBoard ranking={state.ranking} />

          <div className="score-card">
            <div className="score-label">Tu Puntaje</div>
            <div className="score-value">{state.score}</div>
          </div>
        </>
      ) : (
        <>
          <div className="results-icon">⚠️</div>
          <h2 className="text-warning">Sesión Cancelada</h2>
          <p>La sesión fue cancelada por el host.</p>
        </>
      )}

      <button onClick={handleReturn} className="btn-return">
        Volver al inicio
      </button>
    </div>
  );
}
