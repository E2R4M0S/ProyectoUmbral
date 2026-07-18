import { useEffect, useRef } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { GameProvider, useGame } from "../../../contexts/GameContext";
import { getSessionById, getSessionProgress, getSessionRanking } from "../../../services/sessionsApi";
import { getRankingByQuiz } from "../../../services/triviaApi";
import { getSessionTeams } from "../../../services/sessionTeamsApi";
import { useSignalR } from "../../../hooks/useSignalR";
import { fetchWithAuth } from "../../../services/api";
import { userManager } from "../../../auth/keycloak";
import { isMyRankingEntry } from "../../../utils/rankingMatch";
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

  // If getSessionById failed on first load (e.g. token not ready → 401), retry once SignalR connects
  useEffect(() => {
    if (state.connectionState !== "Connected" || state.sessionStatus !== null || !sessionId) return;
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
  }, [state.connectionState, state.sessionStatus, sessionId, dispatch]);

  // Fallback polling for session status changes (when SignalR isn't available, e.g. APK via tunnel).
  // Polls every 4s regardless of current status so that "Preparing" → "Active" is detected.
  useEffect(() => {
    if (!sessionId) return;
    const poll = setInterval(async () => {
      try {
        const session = await getSessionById(sessionId);
        const newStatus = session.status as SessionStatus;
        if (newStatus !== sessionStatusRef.current) {
          dispatch({ type: "STATUS_CHANGED", status: newStatus });
        }
      } catch { /* ignore */ }
    }, 4000);
    return () => clearInterval(poll);
  }, [sessionId, dispatch]);

  // Fallback polling for trivia questions (when SignalR isn't available, e.g. APK via tunnel).
  // Polls every 4s for the current active question via HTTP endpoint.
  // Tracks lastQuestionId ref to detect new questions vs the same question already shown.
  const lastQuestionIdRef = useRef<string | null>(null);
  useEffect(() => {
    if (!sessionId || state.currentMissionType !== "Trivia") return;
    const poll = setInterval(async () => {
      try {
        const resp = await fetchWithAuth(`/api/quizzes/questions/current/${sessionId}`);
        if (!resp.ok) return;
        const data = await resp.json();
        if (data.hasQuestion) {
          if (data.questionId !== lastQuestionIdRef.current) {
            lastQuestionIdRef.current = data.questionId;
            dispatch({ type: "QUESTION_RECEIVED", question: data });
          }
        } else {
          if (lastQuestionIdRef.current !== null) {
            lastQuestionIdRef.current = null;
            dispatch({ type: "QUESTION_CLEARED" });
          }
        }
      } catch { /* ignore */ }
    }, 4000);
    return () => clearInterval(poll);
  }, [sessionId, state.currentMissionType, dispatch]);

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

  // Fallback polling for ranking/score updates (when SignalR isn't available, e.g. APK via tunnel).
  // Polls every 5s and updates the score badge if it changed.
  // Uses refs to avoid stale closures (myUserId/myTeam may not be set when the interval first runs).
  const myUserIdRef = useRef(state.myUserId);
  useEffect(() => { myUserIdRef.current = state.myUserId; }, [state.myUserId]);
  const myTeamRef = useRef(state.myTeam);
  useEffect(() => { myTeamRef.current = state.myTeam; }, [state.myTeam]);
  const lastScoreRef = useRef<number>(state.score);
  useEffect(() => {
    if (!sessionId) return;
    const poll = setInterval(async () => {
      try {
        const rankingRaw = await getSessionRanking(sessionId);
        const ranking = rankingRaw.map(r => ({
          position: r.position,
          teamName: r.displayName,
          score: r.score,
          userId: r.userId ?? undefined,
        }));
        if (ranking.length > 0) {
          const mine = ranking.find(e => isMyRankingEntry(e, myUserIdRef.current, myTeamRef.current));
          if (mine && mine.score !== lastScoreRef.current) {
            lastScoreRef.current = mine.score;
            dispatch({ type: "SET_SCORE", score: mine.score });
            try { sessionStorage.setItem(`score_${sessionId}`, String(mine.score)); } catch { /* ignore */ }
          }
        }
        dispatch({ type: "RANKING_UPDATED", ranking });
      } catch { /* ignore */ }
    }, 5000);
    return () => clearInterval(poll);
  }, [sessionId, dispatch]);

  // Sync elapsedSeconds from the server every 5s (same source the operator dashboard polls),
  // so every participant's mission timer counts down from the exact same numbers instead of
  // drifting apart on local per-client clocks.
  //
  // Depending on sessionStatus (not just sessionId) matters: on first mount/rejoin, status is
  // still null for a beat while SESSION_LOADED is in flight, so a fire-once-at-mount sync would
  // see status===null, bail, and leave currentMissionElapsedSeconds at its initial 0 — showing
  // the full mission duration — until the next 5s tick caught up. Re-running this effect the
  // moment status actually becomes Active/Paused fires the correction immediately instead.
  useEffect(() => {
    if (!sessionId) return;
    const status = state.sessionStatus;
    if (status !== "Active" && status !== "Paused") return;
    const sync = async () => {
      try {
        const progress = await getSessionProgress(sessionId);
        dispatch({
          type: "ELAPSED_SYNCED",
          elapsedSeconds: progress.elapsedSeconds,
          currentMissionElapsedSeconds: progress.currentMissionElapsedSeconds,
        });
      } catch { /* ignore */ }
    };
    sync();
    const poll = setInterval(sync, 5000);
    return () => clearInterval(poll);
  }, [sessionId, dispatch, state.sessionStatus]);

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
    onClueReleased: (clue: unknown, teamId?: string, userId?: string) => {
      // Broadcast (no target) always applies; otherwise it must match my own team or my own user.
      const isForEveryone = !teamId && !userId;
      const isForMyTeam = !!teamId && !!state.myTeam && teamId.toLowerCase() === state.myTeam.id.toLowerCase();
      const isForMe = !!userId && !!state.myUserId && userId.toLowerCase() === state.myUserId.toLowerCase();
      if (isForEveryone || isForMyTeam || isForMe) {
        dispatch({ type: "CLUE_RELEASED", clue });
      }
    },
    onConnectionStateChange: (connState) => {
      dispatch({ type: "CONNECTION_STATE_CHANGED", state: connState });
    },
    onRankingUpdated: (ranking: RankingEntry[]) => {
      // Trivia-only leaderboard from Trivia.Service. Used to drive the leaderboard UI
      // (RankingBoard) during trivia play. The score badge is NOT updated from this event:
      // the trivia leaderboard is per-quiz, so its totals diverge from the accumulated
      // session score (treasure + trivia). See onSessionRankingUpdated for that.
      dispatch({ type: "RANKING_UPDATED", ranking });
    },
    onSessionRankingUpdated: (ranking: RankingEntry[]) => {
      // Authoritative session-wide ranking (treasure + trivia) from Sessions.Service.
      // Replaces the leaderboard with the full-session ordering and updates the score
      // badge to the participant's accumulated total.
      dispatch({ type: "RANKING_UPDATED", ranking });
      const mine = ranking.find(e => isMyRankingEntry(e, state.myUserId, state.myTeam));
      if (mine && mine.score !== state.score) {
        dispatch({ type: "SET_SCORE", score: mine.score });
        if (sessionId) {
          try { sessionStorage.setItem(`score_${sessionId}`, String(mine.score)); } catch { /* ignore */ }
        }
      }
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

        // Load the current user's identity (always) and team membership (if any) — myUserId
        // must be set even for solo participants, since the score badge and ranking "me"
        // highlight both key off it regardless of team status.
        try {
          const user = await userManager.getUser();
          const userId = user?.profile?.sub as string | undefined;
          if (userId && sessionId) {
            const allTeams = await getSessionTeams(sessionId);
            const myTeam = allTeams.find(t => t.members.some(m => m.userId === userId));
            let teamData = null;
            if (myTeam) {
              teamData = { id: myTeam.id, name: myTeam.name, memberIds: myTeam.members.map(m => m.userId) };
              try { sessionStorage.setItem(`myTeam_${sessionId}`, JSON.stringify(teamData)); } catch { /* ignore */ }
            }
            dispatch({ type: "MY_IDENTITY_LOADED", userId, team: teamData });

            // Viewing a past session from "Mis Sesiones" (not just-finished live play):
            // GameContext/sessionStorage has nothing for it, so pull the real result from
            // the backend rather than showing a blank/zeroed results screen.
            const isTerminal = session.status === "Finished" || session.status === "Cancelled";
            const hasCachedRanking = sessionStorage.getItem(`ranking_${sessionId}`);
            if (isTerminal && !hasCachedRanking) {
              try {
                const entries = await getSessionRanking(sessionId);
                const ranking = entries.map(e => ({
                  position: e.position,
                  teamName: e.displayName,
                  score: e.score,
                  userId: e.userId ?? undefined,
                }));
                dispatch({ type: "RANKING_UPDATED", ranking });
                const mine = ranking.find(e => isMyRankingEntry(e, userId ?? null, teamData));
                if (mine) dispatch({ type: "SET_SCORE", score: mine.score });
              } catch { /* ignore — results screen still renders with whatever it has */ }
            }
            if (isTerminal && session.startedAt) {
              const end = session.endedAt ? new Date(session.endedAt).getTime() : Date.now();
              const elapsedSeconds = Math.max(0, Math.round((end - new Date(session.startedAt).getTime()) / 1000));
              dispatch({ type: "ELAPSED_SYNCED", elapsedSeconds, currentMissionElapsedSeconds: 0 });
            }
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
