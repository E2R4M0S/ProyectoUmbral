import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useGame } from "../../../contexts/GameContext";

function formatTime(seconds: number): string {
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${m}:${s.toString().padStart(2, "0")}`;
}

export function GameResults() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const { state, dispatch } = useGame();
  const navigate = useNavigate();

  const [elapsed, setElapsed] = useState(0);
  const [cluesUsed, setCluesUsed] = useState(0);

  const isFinished = state.sessionStatus === "Finished";

  // Restore all result data from sessionStorage
  useEffect(() => {
    if (!sessionId) return;
    const savedScore = sessionStorage.getItem(`score_${sessionId}`);
    if (savedScore) dispatch({ type: "SET_SCORE", score: parseInt(savedScore, 10) });

    const savedRanking = sessionStorage.getItem(`ranking_${sessionId}`);
    if (savedRanking) dispatch({ type: "RANKING_UPDATED", ranking: JSON.parse(savedRanking) });

    const savedElapsed = sessionStorage.getItem(`elapsed_${sessionId}`);
    setElapsed(savedElapsed ? parseInt(savedElapsed, 10) : state.elapsedSeconds);

    const savedClues = sessionStorage.getItem(`clues_${sessionId}`);
    setCluesUsed(savedClues ? (JSON.parse(savedClues) as unknown[]).length : state.clues.length);
  }, [sessionId]);

  const myPosition = state.myUserId
    ? state.ranking.findIndex(r => r.userId === state.myUserId) + 1
    : 0;

  const totalPlayers = state.ranking.length;

  function handleReturn() {
    if (state.sessionId) sessionStorage.removeItem(`joined_${state.sessionId}`);
    navigate("/", { replace: true });
  }

  const medalColor = (pos: number) =>
    pos === 1 ? "#fbbf24" : pos === 2 ? "#9ca3af" : pos === 3 ? "#cd7f32" : "#e94560";

  return (
    <div className="game-results">
      {isFinished ? (
        <>
          <div className="results-icon">🏆</div>
          <h2 style={{ color: "#e94560", margin: "0 0 0.25rem" }}>¡Juego Terminado!</h2>
          <p style={{ color: "#888", margin: "0 0 1.5rem" }}>La experiencia ha finalizado.</p>

          {/* Personal stats */}
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr", gap: "0.75rem", marginBottom: "1.5rem" }}>
            <div style={{ backgroundColor: "#16213e", border: "1px solid #0f3460", borderRadius: 8, padding: "0.75rem", textAlign: "center" }}>
              <div style={{ fontSize: "1.75rem", fontWeight: 700, color: "#e94560" }}>{state.score}</div>
              <div style={{ fontSize: "0.72rem", color: "#888", textTransform: "uppercase", letterSpacing: 1 }}>Puntos</div>
            </div>
            <div style={{ backgroundColor: "#16213e", border: "1px solid #0f3460", borderRadius: 8, padding: "0.75rem", textAlign: "center" }}>
              <div style={{ fontSize: "1.75rem", fontWeight: 700, color: myPosition ? medalColor(myPosition) : "#555" }}>
                {myPosition ? `#${myPosition}` : "—"}
              </div>
              <div style={{ fontSize: "0.72rem", color: "#888", textTransform: "uppercase", letterSpacing: 1 }}>
                {totalPlayers > 0 ? `de ${totalPlayers}` : "Posición"}
              </div>
            </div>
            <div style={{ backgroundColor: "#16213e", border: "1px solid #0f3460", borderRadius: 8, padding: "0.75rem", textAlign: "center" }}>
              <div style={{ fontSize: "1.75rem", fontWeight: 700, color: "#34d399" }}>{formatTime(elapsed || state.elapsedSeconds)}</div>
              <div style={{ fontSize: "0.72rem", color: "#888", textTransform: "uppercase", letterSpacing: 1 }}>Tiempo</div>
            </div>
          </div>

          {cluesUsed > 0 && (
            <div style={{ backgroundColor: "#16213e", border: "1px solid #0f3460", borderRadius: 8, padding: "0.6rem 1rem", marginBottom: "1.5rem", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
              <span style={{ color: "#ccc", fontSize: "0.875rem" }}>Pistas utilizadas</span>
              <span style={{ color: "#fbbf24", fontWeight: 700 }}>{cluesUsed}</span>
            </div>
          )}

          {/* Ranking */}
          {state.ranking.length > 0 && (
            <>
              <div style={{ fontSize: "0.75rem", fontWeight: 700, color: "#e94560", textTransform: "uppercase", letterSpacing: 1, marginBottom: "0.5rem" }}>
                Ranking Final
              </div>
              <div style={{ backgroundColor: "#16213e", border: "1px solid #0f3460", borderRadius: 8, overflow: "hidden", marginBottom: "1.5rem" }}>
                {state.ranking.map((entry, i) => {
                  const isMe = state.myUserId && entry.userId === state.myUserId;
                  return (
                    <div key={i} style={{
                      display: "flex", alignItems: "center", justifyContent: "space-between",
                      padding: "0.6rem 1rem",
                      borderBottom: i < state.ranking.length - 1 ? "1px solid #0f3460" : "none",
                      backgroundColor: isMe ? "#1a2d4a" : "transparent",
                    }}>
                      <div style={{ display: "flex", alignItems: "center", gap: "0.75rem" }}>
                        <span style={{ fontWeight: 700, minWidth: 24, color: medalColor(entry.position) }}>
                          #{entry.position}
                        </span>
                        <span style={{ color: isMe ? "white" : "#ccc", fontSize: "0.9rem", fontWeight: isMe ? 700 : 400 }}>
                          {entry.teamName}{isMe ? " (tú)" : ""}
                        </span>
                      </div>
                      <span style={{ fontWeight: 700, color: "#e94560" }}>{entry.score} pts</span>
                    </div>
                  );
                })}
              </div>
            </>
          )}
        </>
      ) : (
        <>
          <div className="results-icon">⚠️</div>
          <h2 style={{ color: "#ffc107" }}>Sesión Cancelada</h2>
          <p style={{ color: "#888" }}>La sesión fue cancelada por el host.</p>
        </>
      )}

      <button onClick={handleReturn} className="btn-return">
        Volver al inicio
      </button>
    </div>
  );
}
