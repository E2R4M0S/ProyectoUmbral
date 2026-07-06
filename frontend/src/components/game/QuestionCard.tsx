import { useState } from "react";
import { fetchWithAuth } from "../../services/api";
import { useGame } from "../../contexts/GameContext";
import { userManager } from "../../auth/keycloak";
import type { TriviaQuestion } from "../../types/game";

interface Props {
  question: TriviaQuestion;
}

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

      const key = `score_${question.sessionId}`;
      const current = parseInt(sessionStorage.getItem(key) || "0", 10);
      const newScore = current + (data.pointsAwarded || 0);
      sessionStorage.setItem(key, newScore.toString());
      dispatch({ type: "SET_SCORE", score: newScore });
    } catch (ex: any) {
      setError(ex?.message ?? "Error de conexión");
    }
  };

  function optionClass(i: number): string {
    const classes = ["question-option"];
    if (sent || state.answersDisabled) classes.push("disabled");
    if (feedback === "correct" && selectedIndex === i) classes.push("feedback-correct");
    else if (feedback === "wrong" && selectedIndex === i) classes.push("feedback-wrong");
    else if (!sent && selectedIndex === i) classes.push("selected");
    return classes.join(" ");
  }

  return (
    <div className="question-card">
      <h3>{question.questionText}</h3>
      <div className="options-list">
        {question.options.map((opt, i) => (
          <button
            key={i}
            onClick={() => handleAnswer(i)}
            disabled={sent || !!state.answersDisabled}
            className={optionClass(i)}
          >
            {opt}
          </button>
        ))}
      </div>

      {feedback === "correct" && (
        <div className="question-feedback correct">
          <span>✅ ¡Correcta!</span>
          <span className="points">+{pointsAwarded} pts</span>
        </div>
      )}
      {feedback === "wrong" && (
        <div className="question-feedback wrong">
          <span>❌ Equivocada</span>
          <span className="points">0 pts</span>
        </div>
      )}

      {error && (
        <div className="question-error">{error}</div>
      )}
    </div>
  );
}
