import { useState, useEffect } from "react";
import { useParams } from "react-router-dom";
import { useAuth } from "react-oidc-context";
import { useGame } from "../../../contexts/GameContext";
import { getSessionProgress } from "../../../services/sessionsApi";
import { getSessionTeams, joinSessionTeam } from "../../../services/sessionTeamsApi";
import type { SessionTeam } from "../../../services/sessionTeamsApi";

export function WaitingRoom({ loading = false }: { loading?: boolean }) {
  const { state, dispatch } = useGame();
  const { sessionId } = useParams<{ sessionId: string }>();
  const auth = useAuth();

  const [participantCount, setParticipantCount] = useState(0);
  const [teams, setTeams] = useState<SessionTeam[]>([]);
  const [myTeamId, setMyTeamId] = useState<string | null>(null);
  const [joining, setJoining] = useState<string | null>(null);
  const [joinError, setJoinError] = useState<string | null>(null);
  const [dots, setDots] = useState(".");

  const pin = sessionId ? sessionStorage.getItem(`pin_${sessionId}`) : null;
  const userId = auth.user?.profile?.sub as string | undefined;

  useEffect(() => {
    const t = setInterval(() => setDots(d => d.length >= 3 ? "." : d + "."), 600);
    return () => clearInterval(t);
  }, []);

  async function loadProgress() {
    if (!sessionId || loading) return;
    try {
      const progress = await getSessionProgress(sessionId);
      setParticipantCount(progress.participants?.length ?? 0);
    } catch { /* ignore */ }
  }

  async function loadTeams() {
    if (!sessionId) return;
    try {
      const data = await getSessionTeams(sessionId);
      setTeams(data);
      if (userId) {
        const mine = data.find(t => t.members.some(m => m.userId === userId));
        setMyTeamId(mine?.id ?? null);
        if (mine) {
          const teamData = { id: mine.id, name: mine.name, memberIds: mine.members.map(m => m.userId) };
          // Persist to both storages so QuestionCard can use it even if context is lost
          try { sessionStorage.setItem(`myTeam_${sessionId}`, JSON.stringify(teamData)); } catch { /* ignore */ }
          try { localStorage.setItem(`myTeam_${sessionId}`, JSON.stringify(teamData)); } catch { /* ignore */ }
          dispatch({ type: "MY_IDENTITY_LOADED", userId, team: teamData });
        }
        // If not found, don't dispatch — preserve existing state.myTeam without stale-closure risk
      }
    } catch { /* ignore */ }
  }

  useEffect(() => {
    loadProgress();
    loadTeams();
    const interval = setInterval(() => { loadProgress(); loadTeams(); }, 4000);
    return () => clearInterval(interval);
  }, [sessionId, loading, userId]); // eslint-disable-line

  async function handleJoin(teamId: string) {
    if (!sessionId) return;
    setJoining(teamId);
    setJoinError(null);
    try {
      await joinSessionTeam(sessionId, teamId);
      await loadTeams();
    } catch (err) {
      setJoinError(err instanceof Error ? err.message : "Error al unirse al equipo.");
    } finally {
      setJoining(null);
    }
  }

  const connColor = state.connectionState === "Connected" ? "var(--color-success)"
    : state.connectionState === "Reconnecting" ? "var(--color-warning)"
    : "var(--color-error)";

  const connLabel = state.connectionState === "Connected" ? "Conectado en tiempo real"
    : state.connectionState === "Reconnecting" ? "Reconectando..."
    : "Sin conexión";

  return (
    <div className="waiting-room">
      <div className="waiting-room-logo">U</div>

      <h2>{state.sessionName || "Sesión de Juego"}</h2>
      <p>{loading ? `Conectando${dots}` : `Esperando que el host inicie${dots}`}</p>

      {pin && <div className="session-pin">{pin}</div>}

      {!loading && participantCount > 0 && (
        <div className="waiting-room-stat">
          <span>👥</span>
          <span>
            <strong>{participantCount}</strong>
            {" "}participante{participantCount !== 1 ? "s" : ""} conectado{participantCount !== 1 ? "s" : ""}
          </span>
        </div>
      )}

      {!loading && teams.length > 0 && (
        <div className="waiting-teams">
          <div className="waiting-teams-title">Equipos</div>

          {myTeamId && (
            <div className="waiting-teams-my-team">
              ✅ Estás en el equipo <strong>{teams.find(t => t.id === myTeamId)?.name}</strong>
            </div>
          )}

          {joinError && <p className="form-hint-error">{joinError}</p>}

          <div className="waiting-teams-list">
            {teams.map(team => {
              const isMine = team.id === myTeamId;
              const isFull = team.memberCount >= team.maxMembers;
              return (
                <div key={team.id} className={`waiting-team-card${isMine ? " is-mine" : ""}`}>
                  <div className="waiting-team-info">
                    <span className="waiting-team-name">{team.name}</span>
                    <span className="waiting-team-count">
                      {team.memberCount}/{team.maxMembers}
                    </span>
                  </div>
                  {team.members.length > 0 && (
                    <div className="waiting-team-members">
                      {team.members.map(m => (
                        <span key={m.userId} className="waiting-team-member">
                          {m.userAlias}
                        </span>
                      ))}
                    </div>
                  )}
                  {!isMine && !myTeamId && (
                    <button
                      className="btn btn-secondary btn-sm"
                      disabled={isFull || joining === team.id}
                      onClick={() => handleJoin(team.id)}
                    >
                      {joining === team.id ? "Uniéndose..." : isFull ? "Equipo lleno" : "Unirse"}
                    </button>
                  )}
                </div>
              );
            })}
          </div>
        </div>
      )}

      <div className="waiting-room-conn">
        <div className="waiting-room-conn-dot" style={{ backgroundColor: connColor }} />
        <span>{connLabel}</span>
      </div>
    </div>
  );
}
