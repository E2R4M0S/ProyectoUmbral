import { useState } from "react";
import { useGame } from "../../contexts/GameContext";
import type { RankingEntry } from "../../types/game";

const medal = (pos: number): string => {
  if (pos === 1) return "🥇";
  if (pos === 2) return "🥈";
  if (pos === 3) return "🥉";
  return `#${pos}`;
};

function podiumClass(pos: number): string {
  if (pos === 1) return "first";
  if (pos === 2) return "second";
  if (pos === 3) return "third";
  return "";
}

interface RankingBoardProps {
  ranking: RankingEntry[];
}

export function RankingBoard({ ranking }: RankingBoardProps) {
  const { state } = useGame();
  const { myUserId, myTeam } = state;
  const [tab, setTab] = useState<"players" | "team">("players");

  if (ranking.length === 0) return null;

  // Team score: sum of scores for entries whose userId is in my team's member list
  const teamScore = myTeam
    ? ranking
        .filter(e => e.userId && myTeam.memberIds.includes(e.userId))
        .reduce((sum, e) => sum + e.score, 0)
    : null;

  // Team ranking position: how many other teams would outscore my team?
  // We don't know other teams' compositions, so we skip this for now.

  return (
    <div className="ranking-board">
      <div className="ranking-board-header">
        <h3>Ranking en vivo</h3>
        {myTeam && (
          <div className="ranking-tabs">
            <button
              className={`ranking-tab${tab === "players" ? " active" : ""}`}
              onClick={() => setTab("players")}
            >
              Jugadores
            </button>
            <button
              className={`ranking-tab${tab === "team" ? " active" : ""}`}
              onClick={() => setTab("team")}
            >
              Mi Equipo
            </button>
          </div>
        )}
      </div>

      {tab === "players" && (
        <div className="ranking-players">
          {ranking.map((entry) => {
            const isMe = myUserId != null && entry.userId === myUserId;
            const podium = podiumClass(entry.position);
            return (
              <div
                key={entry.position}
                className={[
                  "ranking-row",
                  entry.position <= 3 ? "podium" : "",
                  podium,
                  isMe ? "me" : "",
                ].filter(Boolean).join(" ")}
              >
                <span className="rank-name">
                  {medal(entry.position)} {entry.teamName}
                  {isMe && <span className="rank-me-tag">tú</span>}
                </span>
                <span className="rank-score">{entry.score} pts</span>
              </div>
            );
          })}
        </div>
      )}

      {tab === "team" && myTeam && (
        <div className="ranking-team-view">
          <div className="ranking-team-card">
            <div className="ranking-team-name">{myTeam.name}</div>
            <div className="ranking-team-total">
              <span className="ranking-team-total-label">Puntaje total</span>
              <span className="ranking-team-total-score">{teamScore} pts</span>
            </div>
          </div>

          <div className="ranking-team-members-title">Miembros</div>
          {myTeam.memberIds.map((memberId) => {
            const entry = ranking.find(e => e.userId === memberId);
            const isMe = memberId === myUserId;
            return (
              <div key={memberId} className={`ranking-row${isMe ? " me" : ""}`}>
                <span className="rank-name">
                  {entry ? `${medal(entry.position)} ${entry.teamName}` : "—"}
                  {isMe && <span className="rank-me-tag">tú</span>}
                </span>
                <span className="rank-score">
                  {entry ? `${entry.score} pts` : "0 pts"}
                </span>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
