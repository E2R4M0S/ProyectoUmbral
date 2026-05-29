import { useEffect, useRef } from "react";
import { HubConnectionBuilder } from "@microsoft/signalr";
import { userManager } from "../auth/keycloak";
import type { ConnectionState } from "../types/game";

const HUB_URL = "/hub/game";
const RECONNECT_DELAYS_MS = [0, 1000, 2000, 4000, 8000, 15000, 30000];

interface UseSignalRCallbacks {
  sessionId: string;
  onStatusChanged: (status: string) => void;
  onProgressUpdated: (data: unknown) => void;
  onClueReleased: (clue: unknown) => void;
  onConnectionStateChange: (state: ConnectionState) => void;
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

    hub.onclose(() => callbacksRef.current.onConnectionStateChange("Disconnected"));
    hub.onreconnecting(() => callbacksRef.current.onConnectionStateChange("Reconnecting"));
    hub.onreconnected(() => callbacksRef.current.onConnectionStateChange("Connected"));

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
