import { useEffect, useRef } from "react";
import { useGame } from "../../contexts/GameContext";

function formatTime(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = seconds % 60;
  return `${String(mins).padStart(2, "0")}:${String(secs).padStart(2, "0")}`;
}

export function Timer() {
  const { state, dispatch } = useGame();
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

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

  return (
    <div style={{ textAlign: "center", padding: "1rem" }}>
      <div style={{
        display: "inline-block",
        padding: "12px 24px",
        backgroundColor: "#16213e",
        borderRadius: 8,
        border: "2px solid #e94560",
        fontFamily: "monospace",
        fontSize: "2rem",
        color: "#e94560",
        fontWeight: "bold",
      }}>
        {formatTime(state.elapsedSeconds)}
      </div>
    </div>
  );
}
