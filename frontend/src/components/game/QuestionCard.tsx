import { useState } from "react";
import { fetchWithAuth } from "../../services/api";
import { useGame } from "../../contexts/GameContext";
import { userManager } from "../../auth/keycloak";
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

const optionBtnStyle = (disabled: boolean, sent: boolean, selected: boolean, feedback: "correct" | "wrong" | null): React.CSSProperties => {
  let bg = "#0f3460";
  let bd = "1px solid #1a1a4e";
  if (feedback === "correct") { bg = "#2d6a4f"; bd = "2px solid #2d6a4f"; }
  else if (feedback === "wrong" && selected) { bg = "#8b0000"; bd = "2px solid #e94560"; }
  else if (sent) { bg = "#555"; }
  else if (selected) { bg = "#e94560"; bd = "2px solid #e94560"; }
  return {
    display: "block", width: "100%", padding: "12px 16px", marginBottom: 8,
    backgroundColor: bg, color: "white", border: bd, borderRadius: 8,
    cursor: disabled || sent ? "not-allowed" : "pointer",
    fontSize: 15, textAlign: "left" as const, fontWeight: selected ? 700 : 400,
  };
};

export function QuestionCard({ question }: Props) {
  const { state, dispatch } = useGame();
  const [selectedIndex, setSelectedIndex] = useState<number | null>(null);
  const [sent, setSent] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<"correct" | "wrong" | null>(null);
  const [pointsAwarded, setPointsAwarded] = useState(0);

  const handleAnswer = async (index: number) => {
    if (sent || state.answersDisabled) return;
    setSelectedIndex(index);
    setError(null);

    try {
      const user = await userManager.getUser();
      const teamName = user?.profile?.name || user?.profile?.preferred_username || "Participante";
      const payload = {
        quizId: question.sessionId,
        teamId: user?.profile?.sub || question.sessionId,
        teamName,
        questionId: question.questionId,
        answerId: "00000000-0000-0000-0000-00000000000" + index,
        timestamp: new Date().toISOString(),
        askedAt: question.askedAt,
        timeLimitSeconds: question.timeLimitSeconds,
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

      const data = await resp.json();
      setSent(true);
      setFeedback(data.isCorrect ? "correct" : "wrong");
      setPointsAwarded(data.pointsAwarded || 0);

      // Update local score
      const key = `score_${question.sessionId}`;
      const current = parseInt(sessionStorage.getItem(key) || "0", 10);
      const newScore = current + (data.pointsAwarded || 0);
      sessionStorage.setItem(key, newScore.toString());
      dispatch({ type: "SET_SCORE", score: newScore });
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
            style={optionBtnStyle(sent || !!state.answersDisabled, sent, selectedIndex === i, feedback)}
          >
            {opt}
          </button>
        ))}
      </div>

      {feedback === "correct" && (
        <div style={{ marginTop: 10, padding: "8px 12px", backgroundColor: "#1a3a2a", borderRadius: 6, border: "1px solid #2d6a4f" }}>
          <span style={{ color: "#2d6a4f", fontWeight: 700, fontSize: 15 }}>✅ ¡Correcta!</span>
          <span style={{ color: "#ccc", fontSize: 13, marginLeft: 8 }}>+{pointsAwarded} pts</span>
        </div>
      )}
      {feedback === "wrong" && (
        <div style={{ marginTop: 10, padding: "8px 12px", backgroundColor: "#3a1a1a", borderRadius: 6, border: "1px solid #8b0000" }}>
          <span style={{ color: "#e94560", fontWeight: 700, fontSize: 15 }}>❌ Equivocada</span>
          <span style={{ color: "#999", fontSize: 13, marginLeft: 8 }}>0 pts</span>
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
