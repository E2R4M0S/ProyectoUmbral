import { useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { GameProvider, useGame } from "../../../contexts/GameContext";
import { getSessionById } from "../../../services/sessionsApi";
import { useSignalR } from "../../../hooks/useSignalR";
import { WaitingRoom } from "./WaitingRoom";
import { ActiveGame } from "./ActiveGame";
import { GameResults } from "./GameResults";
import type { SessionStatus } from "../../../types/session";

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
  });

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
      .catch(() => {
        // If session can't be fetched, the guard protects us — stay here
      });
  }, [sessionId, navigate, dispatch]);

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
