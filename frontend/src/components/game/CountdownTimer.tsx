import { useEffect, useRef, useState } from "react";
import { useGame } from "../../contexts/GameContext";

interface Props {
  timeLimitSeconds: number;
  onExpired?: () => void;
}

export function CountdownTimer({ timeLimitSeconds, onExpired }: Props) {
  const { state, dispatch } = useGame();
  const [remaining, setRemaining] = useState(timeLimitSeconds);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    // Restart the countdown whenever the provided time limit changes or when session status changes.
    setRemaining(timeLimitSeconds);
    if (intervalRef.current) {
      clearInterval(intervalRef.current);
      intervalRef.current = null;
    }

    intervalRef.current = setInterval(() => {
      setRemaining((r) => {
        if (r <= 1) {
          if (intervalRef.current) {
            clearInterval(intervalRef.current);
            intervalRef.current = null;
          }
          onExpired?.();
          return 0;
        }
        return r - 1;
      });
    }, 1000);

    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
    };
  }, [timeLimitSeconds, onExpired, state.sessionStatus]);

  return (
    <div style={{ textAlign: "center", marginTop: "1rem" }}>
      <div style={{ fontSize: "2rem", fontFamily: "monospace", color: "#e94560" }}>
        {String(remaining).padStart(2, "0")}s
      </div>
    </div>
  );
}
