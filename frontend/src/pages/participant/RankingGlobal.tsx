import { useState, useEffect } from "react";
import { useAuth } from "react-oidc-context";
import { getUnifiedRanking, type UnifiedRankingEntry as GlobalRankingEntry } from "../../services/sessionsApi";

const medal = (pos: number): string => {
  if (pos === 1) return "🥇";
  if (pos === 2) return "🥈";
  if (pos === 3) return "🥉";
  return `#${pos}`;
};

type Period = "all" | "monthly";

export function RankingGlobal() {
  const auth = useAuth();
  const myUserId = auth.user?.profile?.sub ?? null;

  const [period, setPeriod] = useState<Period>("all");
  const [entries, setEntries] = useState<GlobalRankingEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    setLoading(true);
    setError("");
    getUnifiedRanking(period)
      .then(setEntries)
      .catch(() => setError("No se pudo cargar el ranking."))
      .finally(() => setLoading(false));
  }, [period]);

  const myPosition = myUserId ? entries.find(e => e.userId === myUserId) : null;

  return (
    <div className="page">
      <div className="page-header">
        <h1 className="page-title">Ranking Global</h1>
        <p className="page-subtitle">Tabla de posiciones acumulada de todos los participantes.</p>
      </div>

      <div className="ranking-global-tabs">
        <button
          className={`btn-tab${period === "all" ? " active" : ""}`}
          onClick={() => setPeriod("all")}
        >
          Todos los tiempos
        </button>
        <button
          className={`btn-tab${period === "monthly" ? " active" : ""}`}
          onClick={() => setPeriod("monthly")}
        >
          Este mes
        </button>
      </div>

      {myPosition && (
        <div className="ranking-my-position">
          <span>{medal(myPosition.position)} Tu posición: #{myPosition.position}</span>
          <span>{myPosition.totalScore} pts</span>
        </div>
      )}

      {loading && (
        <div style={{ textAlign: "center", padding: "3rem 0" }}>
          <div className="spinner" />
        </div>
      )}

      {error && <div className="alert alert-error">{error}</div>}

      {!loading && !error && entries.length === 0 && (
        <div className="ranking-empty">
          <div className="ranking-empty-icon">🏆</div>
          <p>Aún no hay puntuaciones registradas.</p>
          <p style={{ fontSize: "0.82rem", marginTop: "0.25rem" }}>
            ¡Participá en una sesión de trivia para aparecer aquí!
          </p>
        </div>
      )}

      {!loading && !error && entries.length > 0 && (
        <div className="ranking-list">
          {entries.map((entry, i) => {
            const isMe = myUserId != null && entry.userId === myUserId;
            const medalColor = i === 0 ? "var(--color-gold)" : i === 1 ? "var(--color-silver)" : i === 2 ? "var(--color-bronze)" : "var(--text-muted)";
            return (
              <div key={entry.userId} className={`ranking-list-row${isMe ? " is-me" : ""}`}>
                <div className="ranking-list-left">
                  <span className="ranking-list-pos" style={{ color: medalColor }}>
                    {medal(entry.position)}
                  </span>
                  <div>
                    <span className="ranking-list-name">{entry.displayName}</span>
                    {isMe && <span className="badge badge-info" style={{ marginLeft: "0.5rem" }}>tú</span>}
                  </div>
                </div>
                <span className="ranking-list-score">{entry.totalScore} pts</span>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
