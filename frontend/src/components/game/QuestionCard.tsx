import { useState, useEffect, useRef } from "react";
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
  const [timeLeft, setTimeLeft] = useState<number>(question.timeLimitSeconds || 30);
  const [correctIndex, setCorrectIndex] = useState<number | null>(null);
  const [questionClosed, setQuestionClosed] = useState(false);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    const limit = question.timeLimitSeconds || 30;
    setTimeLeft(limit);

    timerRef.current = setInterval(() => {
      setTimeLeft(t => {
        if (t <= 1) {
          if (timerRef.current) clearInterval(timerRef.current);
          return 0;
        }
        return t - 1;
      });
    }, 1000);

    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
    };
  }, [question.questionId]);

  useEffect(() => {
    if (sent && timerRef.current) {
      clearInterval(timerRef.current);
    }
  }, [sent]);

  useEffect(() => {
    const handler = (e: Event) => {
      const detail = (e as CustomEvent<{ questionId: string; correctAnswerId: string }>).detail;
      if (detail.questionId !== question.questionId) return;
      const lastChar = detail.correctAnswerId[detail.correctAnswerId.length - 1];
      const idx = parseInt(lastChar, 10);
      setCorrectIndex(isNaN(idx) ? null : idx);
      setQuestionClosed(true);
      if (timerRef.current) clearInterval(timerRef.current);
    };
    window.addEventListener("QuestionClosed", handler);
    return () => window.removeEventListener("QuestionClosed", handler);
  }, [question.questionId]);

  const isTimedOut = timeLeft === 0 && !sent;
  const isDisabled = sent || !!state.answersDisabled || isTimedOut || questionClosed;
  const limit = question.timeLimitSeconds || 30;
  const timerPercent = (timeLeft / limit) * 100;
  const timerColor = timeLeft <= 5 ? "#e94560" : timeLeft <= 10 ? "#fbbf24" : "#34d399";

  const handleAnswer = async (index: number) => {
    if (isDisabled) return;
    setSelectedIndex(index);
    setError(null);

    if (timerRef.current) clearInterval(timerRef.current);

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
    } catch (ex: unknown) {
      setError((ex as Error)?.message ?? "Error de conexión");
    }
  };

  function optionClass(i: number): string {
    const classes = ["question-option"];
    if (isDisabled) classes.push("disabled");
    if (questionClosed && correctIndex === i) classes.push("feedback-correct");
    else if (feedback === "correct" && selectedIndex === i) classes.push("feedback-correct");
    else if (feedback === "wrong" && selectedIndex === i) classes.push("feedback-wrong");
    else if (!sent && !questionClosed && selectedIndex === i) classes.push("selected");
    return classes.join(" ");
  }

  return (
    <div className="question-card">
      {/* Countdown bar */}
      <div className="question-timer-bar-wrap">
        <div
          className="question-timer-bar"
          style={{ width: `${timerPercent}%`, backgroundColor: timerColor }}
        />
      </div>

      <div className="question-timer-label">
        {isTimedOut ? (
          <span style={{ color: "#e94560", fontWeight: 700 }}>¡Tiempo!</span>
        ) : sent ? (
          <span style={{ color: "#34d399" }}>Respuesta enviada</span>
        ) : (
          <span style={{ color: timerColor, fontWeight: timeLeft <= 10 ? 700 : 400 }}>
            {timeLeft}s
          </span>
        )}
      </div>

      <h3>{question.questionText}</h3>
      <div className="options-list">
        {question.options.map((opt, i) => (
          <button
            key={i}
            onClick={() => handleAnswer(i)}
            disabled={isDisabled}
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
      {isTimedOut && !feedback && !questionClosed && (
        <div className="question-feedback wrong">
          <span>⏰ Sin respuesta</span>
          <span className="points">0 pts</span>
        </div>
      )}
      {questionClosed && !feedback && (
        <div className="question-feedback wrong">
          <span>⏰ Ronda terminada</span>
          <span className="points">0 pts</span>
        </div>
      )}

      {error && <div className="question-error">{error}</div>}
    </div>
  );
}
