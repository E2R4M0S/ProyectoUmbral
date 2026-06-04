import { useState } from "react";
import { fetchWithAuth } from "../../services/api";
import type { TriviaQuestion } from "../../types/game";

interface Props {
  question: TriviaQuestion;
}

const cardStyle: React.CSSProperties = {
  backgroundColor: "#16213e",
  borderRadius: 12,
  padding: "1.5rem",
  marginBottom: "1rem",
  border: "1px solid #0f3460",
};

const optionBtnStyle = (disabled: boolean, sent: boolean, selected: boolean): React.CSSProperties => ({
  display: "block",
  width: "100%",
  padding: "12px 16px",
  marginBottom: 8,
  backgroundColor: sent ? "#2d6a4f" : selected ? "#e94560" : "#0f3460",
  color: "white",
  border: selected && !sent ? "2px solid #e94560" : "1px solid #1a1a4e",
  borderRadius: 8,
  cursor: disabled || sent ? "not-allowed" : "pointer",
  fontSize: 15,
  textAlign: "left" as const,
  fontWeight: selected ? 700 : 400,
});

export function QuestionCard({ question }: Props) {
  const [selectedIndex, setSelectedIndex] = useState<number | null>(null);
  const [sent, setSent] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleAnswer = async (index: number) => {
    if (sent || state.answersDisabled) return;
    setSelectedIndex(index);
    setError(null);

    try {
      const payload = {
        quizId: question.sessionId,
        teamId: question.sessionId,
        questionId: question.questionId,
        answerId: "00000000-0000-0000-0000-00000000000" + index,
        timestamp: new Date().toISOString(),
      };

      const resp = await fetchWithAuth("/api/trivia/answers", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      });

      if (!resp.ok) {
        const txt = await resp.text().catch(() => "");
        setError(txt || `Error ${resp.status}`);
        return;
      }

      setSent(true);
    } catch (ex: any) {
      setError(ex?.message ?? "Error de conexión");
    }
  };

  return (
    <div style={cardStyle}>
      <h3 style={{ color: "#e94560", margin: "0 0 1rem", fontSize: 18 }}>
        {question.questionText}
      </h3>
      <div>
        {question.options.map((opt, i) => (
          <button
            key={i}
            onClick={() => handleAnswer(i)}
            disabled={sent || state.answersDisabled}
            style={optionBtnStyle(sent || !!state.answersDisabled, sent, selectedIndex === i)}
          >
            {opt}
          </button>
        ))}
      </div>
      {sent && (
        <div style={{ marginTop: 8, color: "#2d6a4f", fontSize: 14 }}>
          ✓ Respuesta enviada
        </div>
      )}
      {error && (
        <div style={{ marginTop: 8, color: "#e94560", fontSize: 14 }}>
          {error}
        </div>
      )}
    </div>
  );
}
