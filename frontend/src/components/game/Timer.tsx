import { useEffect, useRef } from "react";
import { useGame } from "../../contexts/GameContext";

function formatTime(seconds: number): string {
  const s = Math.max(0, seconds);
  const mins = Math.floor(s / 60);
  const secs = s % 60;
  return `${String(mins).padStart(2, "0")}:${String(secs).padStart(2, "0")}`;
}

export function Timer() {
  const { state, dispatch } = useGame();
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const hasLimit = state.timeLimitSeconds > 0;
  const remaining = state.timeLimitSeconds - state.elapsedSeconds;
  const isUrgent = hasLimit && remaining <= 60 && remaining > 0;
  const isTimeUp = hasLimit && remaining <= 0;

  useEffect(() => {
    intervalRef.current = setInterval(() => {
      dispatch({ type: "TICK" });
    }, 1000);

    return () => {
      if (intervalRef.current !== null) {
        clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
    };
  }, [dispatch]);

  if (!hasLimit || state.currentMissionType === "Trivia") return null;

  return (
    <div className="timer">
      <div
        className="timer-display"
        style={isUrgent ? { color: "#e94560" } : isTimeUp ? { color: "#555" } : undefined}
      >
        {isTimeUp ? "00:00" : formatTime(remaining)}
      </div>
      <div style={{ fontSize: 10, color: "#666", textAlign: "center", marginTop: 2 }}>
        {isTimeUp ? "Tiempo agotado" : "restante"}
      </div>
    </div>
  );
}
