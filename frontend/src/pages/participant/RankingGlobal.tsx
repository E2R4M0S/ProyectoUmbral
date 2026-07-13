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
    <div style={{ maxWidth: 600, margin: "0 auto", color: "white", padding: "0 0 3rem" }}>
      <h2 style={{ fontSize: "1.4rem", fontWeight: 700, color: "#e94560", marginBottom: "0.25rem" }}>
        Ranking Global
      </h2>
      <p style={{ color: "#666", fontSize: "0.875rem", marginBottom: "1.5rem" }}>
        Tabla de posiciones acumulada de todos los participantes.
      </p>

      {/* Period tabs */}
      <div style={{ display: "flex", gap: "0.5rem", marginBottom: "1.5rem" }}>
        <button
          onClick={() => setPeriod("all")}
          style={{
            padding: "8px 20px", borderRadius: 20, border: "none", cursor: "pointer",
            fontWeight: 700, fontSize: "0.875rem",
            backgroundColor: period === "all" ? "#e94560" : "#16213e",
            color: period === "all" ? "white" : "#888",
          }}
        >
          Todos los tiempos
        </button>
        <button
          onClick={() => setPeriod("monthly")}
          style={{
            padding: "8px 20px", borderRadius: 20, border: "none", cursor: "pointer",
            fontWeight: 700, fontSize: "0.875rem",
            backgroundColor: period === "monthly" ? "#e94560" : "#16213e",
            color: period === "monthly" ? "white" : "#888",
          }}
        >
          Este mes
        </button>
      </div>

      {/* My position banner */}
      {myPosition && (
        <div style={{
          backgroundColor: "#1a0d2e", border: "1px solid #9333ea",
          borderRadius: 8, padding: "0.75rem 1rem", marginBottom: "1rem",
          display: "flex", alignItems: "center", justifyContent: "space-between",
        }}>
          <span style={{ color: "#c084fc", fontWeight: 700, fontSize: "0.875rem" }}>
            Tu posición: {medal(myPosition.position)} #{myPosition.position}
          </span>
          <span style={{ color: "#c084fc", fontWeight: 700 }}>{myPosition.totalScore} pts</span>
        </div>
      )}

      {loading && (
        <div style={{ textAlign: "center", color: "#555", padding: "3rem 0" }}>
          <div className="spinner" />
        </div>
      )}

      {error && (
        <div style={{
          backgroundColor: "#2d1a1a", border: "1px solid #e94560",
          borderRadius: 8, padding: "1rem", color: "#e94560", fontSize: "0.875rem",
        }}>
          {error}
        </div>
      )}

      {!loading && !error && entries.length === 0 && (
        <div style={{
          textAlign: "center", color: "#555", padding: "3rem 0",
          backgroundColor: "#16213e", borderRadius: 10, border: "1px solid #0f3460",
        }}>
          <div style={{ fontSize: "2rem", marginBottom: "0.5rem" }}>🏆</div>
          <p>Aún no hay puntuaciones registradas.</p>
          <p style={{ fontSize: "0.82rem" }}>¡Participá en una sesión de trivia para aparecer aquí!</p>
        </div>
      )}

      {!loading && !error && entries.length > 0 && (
        <div style={{ backgroundColor: "#16213e", border: "1px solid #0f3460", borderRadius: 10, overflow: "hidden" }}>
          {entries.map((entry, i) => {
            const isMe = myUserId != null && entry.userId === myUserId;
            return (
              <div
                key={entry.userId}
                style={{
                  display: "flex", alignItems: "center", justifyContent: "space-between",
                  padding: "0.75rem 1rem",
                  borderBottom: i < entries.length - 1 ? "1px solid #0f3460" : "none",
                  backgroundColor: isMe ? "#1a0d2e" : "transparent",
                }}
              >
                <div style={{ display: "flex", alignItems: "center", gap: "0.75rem" }}>
                  <span style={{
                    fontWeight: 700, minWidth: 32, fontSize: "1.1rem",
                    color: i === 0 ? "#fbbf24" : i === 1 ? "#9ca3af" : i === 2 ? "#cd7f32" : "#555",
                  }}>
                    {medal(entry.position)}
                  </span>
                  <div>
                    <span style={{ color: isMe ? "#c084fc" : "white", fontWeight: isMe ? 700 : 500, fontSize: "0.9rem" }}>
                      {entry.displayName}
                    </span>
                    {isMe && (
                      <span style={{
                        marginLeft: 8, fontSize: "0.72rem", fontWeight: 700,
                        backgroundColor: "#9333ea", color: "white",
                        padding: "2px 7px", borderRadius: 10,
                      }}>
                        tú
                      </span>
                    )}
                  </div>
                </div>
                <span style={{ fontWeight: 700, color: isMe ? "#c084fc" : "#e94560", fontSize: "0.9rem" }}>
                  {entry.totalScore} pts
                </span>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
