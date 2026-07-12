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
    onGateOpened: (_nextStageIndex: number) => {
      dispatch({ type: "GATE_OPENED" });
      try { sessionStorage.removeItem(`gate_waiting_${sessionId}`); } catch { /* ignore */ }
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
    case null:
      return <WaitingRoom loading />;
    case "Scheduled":
    case "Preparing":
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
        const currentStage = session.stages.find(s => s.order === session.currentStageOrder)
          ?? session.stages[0]
          ?? null;
        const totalStages = session.stages.length;
        dispatch({
          type: "SESSION_LOADED",
          name: session.name,
          status: session.status,
          missionType: currentStage?.missionType ?? null,
          stageOrder: session.currentStageOrder,
          totalStages,
        });
        // Restore participant's personal stage progress saved from previous QR scan
        try {
          const savedStage = sessionStorage.getItem(`participantStage_${sessionId}`);
          if (savedStage) {
            const { participantStageOrder } = JSON.parse(savedStage);
            dispatch({ type: "STAGE_ADVANCED", participantStageOrder, totalStages });
          }
        } catch { /* ignore */ }
      })
      .catch(() => {});
  }, [sessionId, navigate, dispatch]);

  // Restore clues, score, ranking and gate-waiting state from sessionStorage on reload
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
      const gateWaiting = sessionStorage.getItem(`gate_waiting_${sessionId}`);
      if (gateWaiting) {
        const { position, threshold } = JSON.parse(gateWaiting);
        dispatch({ type: "GATE_REACHED", position, threshold });
      }
    } catch { /* ignore */ }
  }, [sessionId, dispatch]);

  return <GameContent />;
}

export function GameView() {
  return (
    <GameProvider>
      <div className="game-container">
        <GameViewInner />
      </div>
    </GameProvider>
  );
}
