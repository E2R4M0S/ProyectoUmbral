import { useEffect, useState } from "react";
import { HubConnectionBuilder } from "@microsoft/signalr";

type AnswerResult = {
  answerId: string;
  text: string;
  count: number;
  percentage: number;
};

export default function QuestionResults({ quizId }: { quizId: string }) {
  const [results, setResults] = useState<AnswerResult[]>([]);

  useEffect(() => {
    const conn = new HubConnectionBuilder()
      .withUrl("/hub/game")
      .withAutomaticReconnect()
      .build();

    conn.on("QuestionResultsUpdated", (data: unknown) => {
      setResults(data as AnswerResult[]);
    });

    conn.start().catch(console.error);
    return () => { conn.stop(); };
  }, [quizId]);

  return (
    <div className="page" style={{ maxWidth: 680 }}>
      <div className="page-header">
        <h1 className="page-title">Resultados de la Pregunta</h1>
      </div>

      {results.length === 0 ? (
        <div className="empty-state">
          <div className="empty-state-icon">📊</div>
          <div className="empty-state-text">Esperando respuestas...</div>
        </div>
      ) : (
        <div className="card">
          <div style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
            {results.map((r) => (
              <div key={r.answerId}>
                <div style={{ display: "flex", justifyContent: "space-between", marginBottom: "0.5rem", fontSize: "0.875rem" }}>
                  <span style={{ fontWeight: 600 }}>{r.text}</span>
                  <span style={{ color: "var(--text-muted)" }}>
                    {r.count} voto{r.count !== 1 ? "s" : ""} ({r.percentage}%)
                  </span>
                </div>
                <div style={{
                  width: "100%", height: 12, background: "var(--bg-elevated)",
                  borderRadius: "var(--radius-full)", overflow: "hidden",
                }}>
                  <div style={{
                    height: "100%",
                    width: `${r.percentage}%`,
                    background: r.percentage >= 50 ? "var(--color-success)" : "var(--accent)",
                    borderRadius: "var(--radius-full)",
                    transition: "width 0.4s ease",
                  }} />
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
