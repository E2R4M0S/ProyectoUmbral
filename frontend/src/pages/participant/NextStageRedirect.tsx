import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useGame } from "../../contexts/GameContext";

// Keys used by trivia that should be removed on transition
const TRIVIA_LOCAL_KEYS = ["selectedAnswerId", "teamName"];
const TRIVIA_SESSION_PREFIX = "trivia_";

export function NextStageRedirect() {
  const navigate = useNavigate();
  const { dispatch } = useGame();

  useEffect(() => {
    function clearTriviaState() {
      try {
        // remove known local keys
        TRIVIA_LOCAL_KEYS.forEach(k => localStorage.removeItem(k));

        // remove session keys that match prefix
        for (let i = 0; i < sessionStorage.length; i++) {
          const key = sessionStorage.key(i) as string;
          if (key && key.startsWith(TRIVIA_SESSION_PREFIX)) {
            sessionStorage.removeItem(key);
          }
        }
      } catch { }
    }

    function onNavigate(e: any) {
      const payload = e.detail as { NextStageUrl: string };

      clearTriviaState();

      // reset application game state
      dispatch({ type: "RESET" });

      if (payload?.NextStageUrl) {
        navigate(payload.NextStageUrl, { replace: true });
      }
    }

    window.addEventListener("NavigateToStage", onNavigate as EventListener);
    return () => window.removeEventListener("NavigateToStage", onNavigate as EventListener);
  }, [navigate, dispatch]);

  return null;
}
