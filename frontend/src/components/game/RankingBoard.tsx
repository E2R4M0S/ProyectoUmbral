import { useGame } from "../../contexts/GameContext";
import type { RankingEntry } from "../../types/game";
import { isMyRankingEntry } from "../../utils/rankingMatch";

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

  if (ranking.length === 0) return null;

  return (
    <div className="ranking-board">
      <div className="ranking-board-header">
        <h3>Ranking en vivo</h3>
      </div>

      <div className="ranking-players">
        {ranking.map((entry) => {
          const isMe = isMyRankingEntry(entry, myUserId, myTeam);
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
    </div>
  );
}
