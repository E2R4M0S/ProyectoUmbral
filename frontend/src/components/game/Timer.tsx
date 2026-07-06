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
    <div className="timer">
      <div className="timer-display">
        {formatTime(state.elapsedSeconds)}
      </div>
    </div>
  );
}
