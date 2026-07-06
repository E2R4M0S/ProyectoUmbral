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
  if (ranking.length === 0) return null;

  return (
    <div className="ranking-board">
      <h3>Ranking en vivo</h3>
      {ranking.map((entry) => {
        const podium = podiumClass(entry.position);
        return (
          <div
            key={entry.position}
            className={`ranking-row${entry.position <= 3 ? " podium" : ""}${podium ? ` ${podium}` : ""}`}
          >
            <span className="rank-name">
              {medal(entry.position)} {entry.teamName}
            </span>
            <span className="rank-score">
              {entry.score} pts
            </span>
          </div>
        );
      })}
    </div>
  );
}
