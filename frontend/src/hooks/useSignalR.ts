import { useEffect, useRef } from "react";
import { HubConnectionBuilder } from "@microsoft/signalr";
import { userManager } from "../auth/keycloak";
import type { ConnectionState, RankingEntry, TriviaQuestion } from "../types/game";

const HUB_URL = "/hub/game";
const RECONNECT_DELAYS_MS = [0, 1000, 2000, 4000, 8000, 15000, 30000];

  interface UseSignalRCallbacks {
    sessionId: string;
    onStatusChanged: (status: string) => void;
    onProgressUpdated: (data: unknown) => void;
    onClueReleased: (clue: unknown) => void;
    onQuestionClosed?: (payload: { questionId: string; correctAnswerId: string; correctAnswerText?: string }) => void;
    onConnectionStateChange: (state: ConnectionState) => void;
    onQuestionAsked?: (question: TriviaQuestion) => void;
    onRankingUpdated?: (ranking: RankingEntry[]) => void;
  }

export function useSignalR(callbacks: UseSignalRCallbacks): void {
  const callbacksRef = useRef(callbacks);
  callbacksRef.current = callbacks;

  useEffect(() => {
    const { sessionId } = callbacks;

    const hub = new HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: async () => {
          const user = await userManager.getUser();
          return user?.access_token ?? "";
        },
      })
      .withAutomaticReconnect(RECONNECT_DELAYS_MS)
      .build();

    hub.on("SessionStatusChanged", (payload: unknown) => {
      const p = payload as { sessionId: string; status: string };
      if (p.sessionId === sessionId) {
        callbacksRef.current.onStatusChanged(p.status);
      }
    });

    hub.on("ProgressUpdated", (payload: unknown) => {
      const p = payload as { sessionId: string; progressData: unknown };
      if (p.sessionId === sessionId) {
        callbacksRef.current.onProgressUpdated(p.progressData);
      }
    });

    hub.on("ClueReleased", (payload: unknown) => {
      const p = payload as { sessionId: string; teamId?: string; clueData: unknown };
      if (p.sessionId === sessionId) {
        callbacksRef.current.onClueReleased(p.clueData);
      }
    });

    hub.on("QuestionClosed", (payload: unknown) => {
      const p = payload as { sessionId?: string; questionId: string; correctAnswerId: string; correctAnswerText?: string };
      const data = { questionId: p.questionId, correctAnswerId: p.correctAnswerId, correctAnswerText: p.correctAnswerText };
      callbacksRef.current.onQuestionClosed?.(data);
      try {
        const ev = new CustomEvent("QuestionClosed", { detail: data });
        window.dispatchEvent(ev);
      } catch { }
    });

    hub.on("QuestionAsked", (payload: unknown) => {
      const p = payload as TriviaQuestion;
      if (p.sessionId === sessionId) {
        callbacksRef.current.onQuestionAsked?.(p);
      }
    });

    hub.on("RankingUpdated", (payload: unknown) => {
      const p = payload as { sessionId: string; ranking: RankingEntry[] };
      if (p.sessionId === sessionId) {
        callbacksRef.current.onRankingUpdated?.(p.ranking);
      }
    });

    hub.onclose(() => callbacksRef.current.onConnectionStateChange("Disconnected"));
    hub.onreconnecting(() => callbacksRef.current.onConnectionStateChange("Reconnecting"));
    hub.onreconnected(async () => {
      callbacksRef.current.onConnectionStateChange("Connected");
      try {
        await hub.invoke("JoinSessionGroup", sessionId);
      } catch { }
    });

    hub.start()
      .then(() => {
        callbacksRef.current.onConnectionStateChange("Connected");
        return hub.invoke("JoinSessionGroup", sessionId);
      })
      .catch(() => {
        callbacksRef.current.onConnectionStateChange("Disconnected");
      });

    return () => {
      hub.invoke("LeaveSessionGroup", sessionId).catch(() => {});
      hub.stop();
    };
  }, [callbacks.sessionId]);
}
