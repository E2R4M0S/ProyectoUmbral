import { useState } from "react";
import { fetchWithAuth } from "../../services/api";
import { useGame } from "../../contexts/GameContext";

interface Props {
  question: any;
}

export function QuestionCard({ question }: Props) {
  const { state } = useGame();
  const [disabled, setDisabled] = useState(false);
  const [sent, setSent] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleAnswer = async (answerId: string) => {
    if (disabled || sent || state.answersDisabled) return;
    setDisabled(true);
    // Optimistically disable answers at global level
    try { dispatch({ type: "SET_ANSWERS_DISABLED", disabled: true }); } catch { }
    setError(null);
    try {
      const payload = {
        quizId: question.quizId,
        teamId: question.teamId ?? "00000000-0000-0000-0000-000000000000",
        questionId: question.id,
        answerId,
        timestamp: new Date().toISOString(),
      };

      const resp = await fetchWithAuth(`/api/trivia/answers`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      });

      if (!resp.ok) {
        const txt = await resp.text().catch(() => "");
        setError(txt || `Error ${resp.status}`);
        setDisabled(false);
        return;
      }

      setSent(true);
      // keep UI blocked until next question; the global `answersDisabled` will be reset when a new clue is released
    } catch (ex: any) {
      setError(ex?.message ?? "Network error");
      setDisabled(false);
      try { dispatch({ type: "SET_ANSWERS_DISABLED", disabled: false }); } catch { }
    }
  };

  return (
    <div style={{ padding: "1rem" }}>
      <div style={{ marginBottom: 8, fontWeight: "bold" }}>{question.text}</div>
      <div style={{ display: "grid", gap: 8 }}>
        {(question.answers ?? []).map((a: any) => (
          <button
            key={a.id}
            onClick={() => handleAnswer(a.id)}
            disabled={disabled || sent || state.answersDisabled}
            style={{
              padding: "8px 12px",
              backgroundColor: sent ? "#2d6a4f" : "#0f3460",
              color: "white",
              borderRadius: 6,
              border: "none",
              cursor: disabled || sent || state.answersDisabled ? "not-allowed" : "pointer",
            }}
          >
            {a.text}
          </button>
        ))}
      </div>
      {sent && <div style={{ marginTop: 8, color: "#2d6a4f" }}>Respuesta Recibida</div>}
      {error && <div style={{ marginTop: 8, color: "#e94560" }}>{error}</div>}
    </div>
  );
}
