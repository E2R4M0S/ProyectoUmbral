import { useState, useEffect } from "react";
import { useParams } from "react-router-dom";
import { useGame } from "../../../contexts/GameContext";
import { getSessionProgress } from "../../../services/sessionsApi";

export function WaitingRoom({ loading = false }: { loading?: boolean }) {
  const { state } = useGame();
  const { sessionId } = useParams<{ sessionId: string }>();
  const [participantCount, setParticipantCount] = useState(0);
  const [dots, setDots] = useState(".");

  const pin = sessionId ? sessionStorage.getItem(`pin_${sessionId}`) : null;

  useEffect(() => {
    const t = setInterval(() => setDots(d => d.length >= 3 ? "." : d + "."), 600);
    return () => clearInterval(t);
  }, []);

  useEffect(() => {
    if (!sessionId || loading) return;
    const poll = async () => {
      try {
        const progress = await getSessionProgress(sessionId);
        setParticipantCount(progress.participants?.length ?? 0);
      } catch { /* ignore */ }
    };
    poll();
    const interval = setInterval(poll, 4000);
    return () => clearInterval(interval);
  }, [sessionId, loading]);

  const connColor = state.connectionState === "Connected" ? "var(--color-success)"
    : state.connectionState === "Reconnecting" ? "var(--color-warning)"
    : "var(--color-error)";

  const connLabel = state.connectionState === "Connected" ? "Conectado en tiempo real"
    : state.connectionState === "Reconnecting" ? "Reconectando..."
    : "Sin conexión";

  return (
    <div className="waiting-room">
      <div className="waiting-room-logo">U</div>

      <h2>{state.sessionName || "Sesión de Juego"}</h2>
      <p>{loading ? `Conectando${dots}` : `Esperando que el host inicie${dots}`}</p>

      {pin && (
        <div className="session-pin">{pin}</div>
      )}

      {!loading && participantCount > 0 && (
        <div className="waiting-room-stat">
          <span>👥</span>
          <span>
            <strong>{participantCount}</strong>
            {" "}participante{participantCount !== 1 ? "s" : ""} conectado{participantCount !== 1 ? "s" : ""}
          </span>
        </div>
      )}

      <div className="waiting-room-conn">
        <div className="waiting-room-conn-dot" style={{ backgroundColor: connColor }} />
        <span>{connLabel}</span>
      </div>
    </div>
  );
}
