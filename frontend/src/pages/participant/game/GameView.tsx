import { useEffect, useRef } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { GameProvider, useGame } from "../../../contexts/GameContext";
import { getSessionById } from "../../../services/sessionsApi";
import { getRankingByQuiz } from "../../../services/triviaApi";
import { getMyTeams } from "../../../services/teamsApi";
import { useSignalR } from "../../../hooks/useSignalR";
import { userManager } from "../../../auth/keycloak";
import { WaitingRoom } from "./WaitingRoom";
import { ActiveGame } from "./ActiveGame";
import { GameResults } from "./GameResults";
import type { SessionStatus } from "../../../types/session";
import type { RankingEntry, TriviaQuestion } from "../../../types/game";

function GameContent() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const { state, dispatch } = useGame();

  // Refs so the polling interval always reads current values without restarting
  const currentStageOrderRef = useRef(state.currentStageOrder);
  useEffect(() => { currentStageOrderRef.current = state.currentStageOrder; });
  const sessionStatusRef = useRef(state.sessionStatus);
  useEffect(() => { sessionStatusRef.current = state.sessionStatus; });

  // Fallback polling: if the SignalR ProgressUpdated event is missed (e.g. Docker not rebuilt),
  // re-fetch session state every 8s and update stage if the operator advanced it.
  useEffect(() => {
    if (!sessionId) return;
    const poll = setInterval(async () => {
      const status = sessionStatusRef.current;
      if (status !== "Active" && status !== "Paused") return;
      try {
        const session = await getSessionById(sessionId);
        if (session.currentStageOrder !== currentStageOrderRef.current) {
          dispatch({ type: "QUESTION_CLEARED" });
          const sorted = [...session.stages].sort((a, b) => a.order - b.order);
          const currentStage = sorted.find(s => s.order === session.currentStageOrder) ?? sorted[0] ?? null;
          dispatch({
            type: "SESSION_LOADED",
            name: session.name,
            status: session.status as SessionStatus,
            missionType: currentStage?.missionType ?? null,
            stageOrder: session.currentStageOrder,
            totalStages: sorted.length,
            stages: sorted,
          });
        }
      } catch { /* ignore */ }
    }, 8000);
    return () => clearInterval(poll);
  }, [sessionId, dispatch]);

  useSignalR({
    sessionId: sessionId!,
    onStatusChanged: (status: string) => {
      dispatch({ type: "STATUS_CHANGED", status: status as SessionStatus });
    },
    onProgressUpdated: (data: unknown) => {
      const p = data as { stageAdvanced?: boolean };
      if (p?.stageAdvanced === true && sessionId) {
        dispatch({ type: "QUESTION_CLEARED" });
        getSessionById(sessionId)
          .then(session => {
            const sorted = [...session.stages].sort((a, b) => a.order - b.order);
            const currentStage = sorted.find(s => s.order === session.currentStageOrder) ?? sorted[0] ?? null;
            dispatch({
              type: "SESSION_LOADED",
              name: session.name,
              status: session.status as SessionStatus,
              missionType: currentStage?.missionType ?? null,
              stageOrder: session.currentStageOrder,
              totalStages: sorted.length,
              stages: sorted,
            });
          })
          .catch(() => {});
      } else {
        dispatch({ type: "PROGRESS_UPDATED", data });
      }
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

  // Force save ranking, score, elapsed time and clues when transitioning to Finished
  useEffect(() => {
    if (sessionId && state.sessionStatus === "Finished") {
      try {
        if (state.ranking.length > 0) {
          sessionStorage.setItem(`ranking_${sessionId}`, JSON.stringify(state.ranking));
        }
        sessionStorage.setItem(`score_${sessionId}`, String(state.score));
        sessionStorage.setItem(`elapsed_${sessionId}`, String(state.elapsedSeconds));
        if (state.clues.length > 0) {
          sessionStorage.setItem(`clues_${sessionId}`, JSON.stringify(state.clues));
        }
      } catch { /* ignore */ }
    }
  }, [state.sessionStatus, sessionId]);

  // Clear score and ranking when session is cancelled
  useEffect(() => {
    if (sessionId && state.sessionStatus === "Cancelled") {
      try {
        sessionStorage.removeItem(`score_${sessionId}`);
        sessionStorage.removeItem(`ranking_${sessionId}`);
      } catch { /* ignore */ }
      dispatch({ type: "SET_SCORE", score: 0 });
      dispatch({ type: "RANKING_UPDATED", ranking: [] });
    }
  }, [state.sessionStatus, sessionId, dispatch]);

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
      .then(async (session) => {
        const sorted = [...session.stages].sort((a, b) => a.order - b.order);
        const currentStage = sorted.find(s => s.order === session.currentStageOrder)
          ?? sorted[0]
          ?? null;
        const totalStages = sorted.length;
        dispatch({
          type: "SESSION_LOADED",
          name: session.name,
          status: session.status,
          missionType: currentStage?.missionType ?? null,
          stageOrder: session.currentStageOrder,
          totalStages,
          stages: sorted,
        });
        // Restore participant's personal stage progress saved from previous QR scan
        try {
          const savedStage = sessionStorage.getItem(`participantStage_${sessionId}`);
          if (savedStage) {
            const { participantStageOrder } = JSON.parse(savedStage);
            dispatch({ type: "STAGE_ADVANCED", participantStageOrder, totalStages });
          }
        } catch { /* ignore */ }
        // Load persisted trivia ranking from DB for trivia sessions
        const triviaStage = sorted.find(s => s.missionType === "Trivia" && s.quizId);
        if (triviaStage?.quizId) {
          const stored = sessionStorage.getItem(`ranking_${sessionId}`);
          if (!stored) {
            try {
              const ranking = await getRankingByQuiz(triviaStage.quizId);
              if (ranking.length > 0) {
                dispatch({ type: "RANKING_UPDATED", ranking });
              }
            } catch { /* ignore */ }
          }
        }

        // Load the current user's identity and team membership
        try {
          const user = await userManager.getUser();
          const userId = user?.profile?.sub as string | undefined;
          if (userId) {
            const teams = await getMyTeams();
            dispatch({
              type: "MY_IDENTITY_LOADED",
              userId,
              team: teams.length > 0 ? teams[0] : null,
            });
          }
        } catch { /* ignore — team info is optional */ }
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
