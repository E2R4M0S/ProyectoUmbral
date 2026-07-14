import { useEffect } from "react";
import { useNavigate } from "react-router-dom";

// Keys used by trivia that should be removed on transition
const TRIVIA_LOCAL_KEYS = ["selectedAnswerId", "teamName"];
const TRIVIA_SESSION_PREFIX = "trivia_";

export function NextStageRedirect() {
  const navigate = useNavigate();

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

      // Reset participant stage progress in sessionStorage so GameView restores fresh
      try { sessionStorage.removeItem(`participantStage_${window.location.pathname.split("/")[2]}`); } catch { }

      if (payload?.NextStageUrl) {
        navigate(payload.NextStageUrl, { replace: true });
      }
    }

    window.addEventListener("NavigateToStage", onNavigate as EventListener);
    return () => window.removeEventListener("NavigateToStage", onNavigate as EventListener);
  }, [navigate]);

  return null;
}
