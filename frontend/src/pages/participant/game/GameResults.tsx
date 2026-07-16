import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useGame } from "../../../contexts/GameContext";
import { isMyRankingEntry } from "../../../utils/rankingMatch";

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
  }, [sessionId]); // eslint-disable-line

  const myPosition = state.ranking.findIndex(r => isMyRankingEntry(r, state.myUserId, state.myTeam)) + 1;
  const totalPlayers = state.ranking.length;

  function handleReturn() {
    if (state.sessionId) sessionStorage.removeItem(`joined_${state.sessionId}`);
    navigate("/", { replace: true });
  }

  function medalColor(pos: number) {
    if (pos === 1) return "var(--color-gold)";
    if (pos === 2) return "var(--color-silver)";
    if (pos === 3) return "var(--color-bronze)";
    return "var(--accent)";
  }

  return (
    <div className="game-results">
      {isFinished ? (
        <>
          <div className="results-icon">🏆</div>
          <h2 className="text-accent">¡Juego Terminado!</h2>
          <p>La experiencia ha finalizado.</p>

          <div className="results-stats-grid">
            <div className="results-stat-card">
              <div className="results-stat-value" style={{ color: "var(--accent)" }}>{state.score}</div>
              <div className="results-stat-label">Puntos</div>
            </div>
            <div className="results-stat-card">
              <div className="results-stat-value" style={{ color: myPosition ? medalColor(myPosition) : "var(--text-muted)" }}>
                {myPosition ? `#${myPosition}` : "—"}
              </div>
              <div className="results-stat-label">{totalPlayers > 0 ? `de ${totalPlayers}` : "Posición"}</div>
            </div>
            <div className="results-stat-card">
              <div className="results-stat-value" style={{ color: "var(--color-success)" }}>
                {formatTime(elapsed || state.elapsedSeconds)}
              </div>
              <div className="results-stat-label">Tiempo</div>
            </div>
          </div>

          {cluesUsed > 0 && (
            <div className="results-clues-row">
              <span>Pistas utilizadas</span>
              <span style={{ color: "var(--color-warning)", fontWeight: 700 }}>{cluesUsed}</span>
            </div>
          )}

          {state.ranking.length > 0 && (
            <>
              <div className="results-ranking-title">Ranking Final</div>
              <div className="results-ranking-list">
                {state.ranking.map((entry, i) => {
                  const isMe = isMyRankingEntry(entry, state.myUserId, state.myTeam);
                  return (
                    <div key={i} className={`results-ranking-row${isMe ? " is-me" : ""}`}>
                      <div style={{ display: "flex", alignItems: "center", gap: "0.75rem" }}>
                        <span style={{ fontWeight: 700, minWidth: 24, color: medalColor(entry.position) }}>
                          #{entry.position}
                        </span>
                        <span style={{ fontSize: "0.9rem", fontWeight: isMe ? 700 : 400 }}>
                          {entry.teamName}{isMe ? " (yo)" : ""}
                        </span>
                      </div>
                      <span style={{ fontWeight: 700, color: "var(--accent)" }}>{entry.score} pts</span>
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
