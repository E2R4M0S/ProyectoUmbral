import { useEffect, useState } from "react";
import { HubConnectionBuilder } from "@microsoft/signalr";

export default function Leaderboard({ quizId }: { quizId: string }) {
  const [entries, setEntries] = useState<Array<{ teamId: string; score: number }>>([]);

  useEffect(() => {
    const conn = new HubConnectionBuilder().withUrl("/hub/game").build();
    conn.start().catch(console.error);

    conn.invoke("JoinSessionGroup", quizId).catch(() => {});

    conn.on("LeaderboardUpdated", (data: any) => {
      setEntries(data.map((e: any) => ({ teamId: e.teamId, score: e.score })));
    });

    return () => {
      conn.invoke("LeaveSessionGroup", quizId).catch(() => {});
      conn.stop().catch(() => {});
    };
  }, [quizId]);

  return (
    <div className="p-4">
      <h2 className="text-xl font-bold mb-2">Leaderboard</h2>
      <ul>
        {entries.map((e) => (
          <li key={e.teamId} className="py-1">
            <span className="font-medium">{e.teamId}</span>: {e.score}
          </li>
        ))}
      </ul>
    </div>
  );
}
