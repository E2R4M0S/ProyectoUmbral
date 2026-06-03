import type { RankingEntry } from "../../types/game";

const boardStyle: React.CSSProperties = {
  backgroundColor: "#16213e",
  borderRadius: 12,
  padding: "1rem",
  marginBottom: "1rem",
  border: "1px solid #0f3460",
};

const rowStyle = (position: number): React.CSSProperties => ({
  display: "flex",
  justifyContent: "space-between",
  padding: "8px 12px",
  marginBottom: 4,
  borderRadius: 8,
  backgroundColor: position <= 3 ? "#0f3460" : "transparent",
  borderLeft: position === 1 ? "4px solid gold" : position === 2 ? "4px solid silver" : position === 3 ? "4px solid #cd7f32" : "4px solid transparent",
});

const medal = (pos: number): string => {
  if (pos === 1) return "🥇";
  if (pos === 2) return "🥈";
  if (pos === 3) return "🥉";
  return `#${pos}`;
};

interface RankingBoardProps {
  ranking: RankingEntry[];
}

export function RankingBoard({ ranking }: RankingBoardProps) {
  if (ranking.length === 0) return null;

  return (
    <div style={boardStyle}>
      <h3 style={{ color: "#e94560", margin: "0 0 0.75rem", fontSize: 16 }}>
        Ranking en vivo
      </h3>
      {ranking.map((entry) => (
        <div key={entry.position} style={rowStyle(entry.position)}>
          <span style={{ color: "#ccc", fontWeight: entry.position <= 3 ? 700 : 400 }}>
            {medal(entry.position)} {entry.teamName}
          </span>
          <span style={{ color: "#e94560", fontWeight: 700 }}>
            {entry.score} pts
          </span>
        </div>
      ))}
    </div>
  );
}
