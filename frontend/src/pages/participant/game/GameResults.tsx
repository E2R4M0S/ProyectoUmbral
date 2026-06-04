import { useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useGame } from "../../../contexts/GameContext";
import { RankingBoard } from "../../../components/game/RankingBoard";

export function GameResults() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const { state, dispatch } = useGame();
  const navigate = useNavigate();

  const isFinished = state.sessionStatus === "Finished";

  // Restore score and ranking from sessionStorage
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

          <RankingBoard ranking={state.ranking} />

          <div style={{
            marginTop: "1rem",
            padding: "1.5rem 2rem",
            backgroundColor: "#16213e",
            borderRadius: 12,
            border: "2px solid #e94560",
            minWidth: 200,
          }}>
            <div style={{ color: "#999", fontSize: "0.9rem", marginBottom: 4 }}>
              Tu Puntaje
            </div>
            <div style={{
              fontSize: "2.5rem",
              fontWeight: "bold",
              color: "#e94560",
            }}>
              {state.score}
            </div>
          </div>
        </> 
      ) : (
        <>
          <div style={{ fontSize: "3rem", marginBottom: "1rem" }}>⚠️</div>
          <h2 style={{ color: "#ffc107", marginBottom: "0.5rem" }}>Sesión Cancelada</h2>
          <p style={{ color: "#999" }}>La sesión fue cancelada por el host.</p>
        </>
      )}

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
