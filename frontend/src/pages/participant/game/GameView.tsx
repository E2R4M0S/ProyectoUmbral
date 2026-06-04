import { useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { GameProvider, useGame } from "../../../contexts/GameContext";
import { getSessionById } from "../../../services/sessionsApi";
import { useSignalR } from "../../../hooks/useSignalR";
import { WaitingRoom } from "./WaitingRoom";
import { ActiveGame } from "./ActiveGame";
import { GameResults } from "./GameResults";
import type { SessionStatus } from "../../../types/session";
import type { RankingEntry, TriviaQuestion } from "../../../types/game";

function GameContent() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const { state, dispatch } = useGame();

  useSignalR({
    sessionId: sessionId!,
    onStatusChanged: (status: string) => {
      dispatch({ type: "STATUS_CHANGED", status: status as SessionStatus });
    },
    onProgressUpdated: (data: unknown) => {
      dispatch({ type: "PROGRESS_UPDATED", data });
    },
    onClueReleased: (clue: unknown) => {
      dispatch({ type: "CLUE_RELEASED", clue });
    },
    onConnectionStateChange: (connState) => {
      dispatch({ type: "CONNECTION_STATE_CHANGED", state: connState });
    },
    onRankingUpdated: (ranking: RankingEntry[]) => {
      dispatch({ type: "RANKING_UPDATED", ranking });
    },
    onQuestionAsked: (question: TriviaQuestion) => {
      dispatch({ type: "QUESTION_RECEIVED", question });
    },
  });

  // Persist clues and ranking to sessionStorage so they survive a reload
  useEffect(() => {
    if (!sessionId) return;
    try {
      if (state.clues.length > 0) {
        sessionStorage.setItem(`clues_${sessionId}`, JSON.stringify(state.clues));
      }
      if (state.ranking.length > 0) {
        sessionStorage.setItem(`ranking_${sessionId}`, JSON.stringify(state.ranking));
      }
    } catch { /* ignore */ }
  }, [state.clues, state.ranking, sessionId]);

  // Force save ranking when transitioning to Finished
  useEffect(() => {
    if (sessionId && state.sessionStatus === "Finished" && state.ranking.length > 0) {
      try {
        sessionStorage.setItem(`ranking_${sessionId}`, JSON.stringify(state.ranking));
        sessionStorage.setItem(`score_${sessionId}`, String(state.score));
      } catch { /* ignore */ }
    }
  }, [state.sessionStatus, sessionId]);

  if (!sessionId) return null;

  switch (state.sessionStatus) {
    case "Scheduled":
    case "Preparing":
    case null:
      return <WaitingRoom />;
    case "Active":
    case "Paused":
      return <ActiveGame />;
    case "Finished":
    case "Cancelled":
      return <GameResults />;
    default:
      return <WaitingRoom />;
  }
}

function GameViewInner() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const navigate = useNavigate();
  const { dispatch } = useGame();

  useEffect(() => {
    if (!sessionId) return;
    const flag = sessionStorage.getItem(`joined_${sessionId}`);
    if (!flag) {
      navigate("/participant/sessions/join", { replace: true });
      return;
    }
    getSessionById(sessionId)
      .then((session) => {
        dispatch({
          type: "SESSION_LOADED",
          name: session.name,
          status: session.status,
        });
      })
      .catch(() => {});
  }, [sessionId, navigate, dispatch]);

  // Restore clues, score and ranking from sessionStorage on reload
  useEffect(() => {
    if (!sessionId) return;
    try {
      const saved = sessionStorage.getItem(`clues_${sessionId}`);
      if (saved) {
        const clues = JSON.parse(saved);
        clues.forEach((clue: unknown) => dispatch({ type: "CLUE_RELEASED", clue }));
      }
      const savedScore = sessionStorage.getItem(`score_${sessionId}`);
      if (savedScore) {
        dispatch({ type: "SET_SCORE", score: parseInt(savedScore, 10) });
      }
      const savedRanking = sessionStorage.getItem(`ranking_${sessionId}`);
      if (savedRanking) {
        dispatch({ type: "RANKING_UPDATED", ranking: JSON.parse(savedRanking) });
      }
    } catch { /* ignore */ }
  }, [sessionId, dispatch]);

  return <GameContent />;
}

export function GameView() {
  return (
    <GameProvider>
      <div style={{
        minHeight: "100vh",
        backgroundColor: "#1a1a2e",
        color: "white",
        fontFamily: "sans-serif",
      }}>
        <GameViewInner />
      </div>
    </GameProvider>
  );
}
