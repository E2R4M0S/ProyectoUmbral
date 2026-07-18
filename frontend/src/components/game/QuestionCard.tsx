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
  const [teamSynced, setTeamSynced] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<"correct" | "wrong" | null>(null);
  const [pointsAwarded, setPointsAwarded] = useState(0);
  const [timeLeft, setTimeLeft] = useState<number>(question.timeLimitSeconds || 30);
  const [correctIndex, setCorrectIndex] = useState<number | null>(null);
  const [questionClosed, setQuestionClosed] = useState(false);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const sentRef = useRef(sent);
  useEffect(() => { sentRef.current = sent; });
  const autoSubmittedRef = useRef(false);
  const myTeamRef = useRef(state.myTeam);
  useEffect(() => { myTeamRef.current = state.myTeam; });
  // Single synchronous gate for "has a submission already been claimed for this question".
  // Four independent paths can all try to record an answer and add points (a direct click, the
  // teammate-answered broadcast, the polling fallback, and the timeout auto-submit) — sentRef
  // alone isn't enough because it's only updated by a useEffect *after* render, so two paths
  // firing within the same tick could both read it as false and each add the points once,
  // doubling the score. This ref is set synchronously, in the same line it's checked, so
  // whichever path runs first atomically wins and every other path bails immediately.
  const submissionClaimedRef = useRef(false);
  function claimSubmission(): boolean {
    if (submissionClaimedRef.current) return false;
    submissionClaimedRef.current = true;
    return true;
  }
  // Reset guards when question changes
  useEffect(() => { autoSubmittedRef.current = false; submissionClaimedRef.current = false; }, [question.questionId]);

  useEffect(() => {
    const limit = Math.max(question.timeLimitSeconds || 30, 10);
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

  // When question closes after team sync, resolve feedback based on correct answer
  useEffect(() => {
    if (!sent || !teamSynced || !questionClosed || feedback !== null || selectedIndex === null || correctIndex === null) return;
    setFeedback(selectedIndex === correctIndex ? "correct" : "wrong");
  }, [sent, teamSynced, questionClosed, feedback, selectedIndex, correctIndex]);

  // Team sync via direct SignalR event: fires immediately when a teammate submits an answer.
  useEffect(() => {
    const handler = (e: Event) => {
      const detail = (e as CustomEvent<{ teamId: string; questionId: string; selectedIndex: number; isCorrect: boolean; pointsAwarded: number }>).detail;
      if (detail.questionId !== question.questionId) return;
      // Use context team first, fall back to sessionStorage in case context was lost
      const myTeamId = state.myTeam?.id ?? (() => {
        try { return JSON.parse(sessionStorage.getItem(`myTeam_${question.sessionId}`) ?? "null")?.id ?? null; } catch { return null; }
      })();
      if (!myTeamId || detail.teamId !== myTeamId) return;
      if (!claimSubmission()) return;
      setSelectedIndex(detail.selectedIndex);
      setSent(true);
      setTeamSynced(true);
      if (timerRef.current) clearInterval(timerRef.current);
      // Show the same feedback and points as the teammate who answered. The running score
      // badge itself is NOT touched here — GameView's ranking-broadcast sync is the single
      // source of truth for it (see GameView.tsx onRankingUpdated), so this only drives the
      // per-question "+N pts" display. Updating it here too used to double-count whenever this
      // broadcast and the ranking sync both landed for the same answer.
      setFeedback(detail.isCorrect ? "correct" : "wrong");
      if (detail.isCorrect && detail.pointsAwarded > 0) {
        setPointsAwarded(detail.pointsAwarded);
      }
      // Submit to backend with own userId so this participant is counted individually.
      (async () => {
        try {
          const user = await userManager.getUser();
          const myUserId = user?.profile?.sub as string | undefined;
          if (!myUserId) return;
          const capturedTeam = myTeamRef.current;
          await fetchWithAuth("/api/trivia/answers", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
              quizId: question.sessionId,
              teamId: detail.teamId,
              teamName: capturedTeam?.name || "Equipo",
              questionId: question.questionId,
              answerId: "00000000-0000-0000-0000-00000000000" + detail.selectedIndex,
              userId: myUserId,
              timestamp: new Date().toISOString(),
              askedAt: question.askedAt,
              timeLimitSeconds: question.timeLimitSeconds,
            }),
          });
        } catch { /* ignore — UI already shows synced state */ }
      })();
    };
    window.addEventListener("TeamAnswerSubmitted", handler);
    return () => window.removeEventListener("TeamAnswerSubmitted", handler);
  }, [question.questionId, state.myTeam?.id]);

  // Team sync fallback: poll every 1.5s until team answers, reading teamId from all possible
  // sources so it works even if context is null. Keeps checking a few times past the round's
  // close and immediately on tab/app foreground, because a suspended phone can miss both the
  // "QuestionClosed" SignalR event and throttled poll ticks — without this, a teammate's
  // answer made while this phone was suspended would be silently lost on resume.
  useEffect(() => {
    const getTeamId = (): string | null => {
      if (state.myTeam?.id) return state.myTeam.id;
      try {
        const ss = sessionStorage.getItem(`myTeam_${question.sessionId}`);
        if (ss) return JSON.parse(ss)?.id ?? null;
      } catch { /* ignore */ }
      try {
        const ls = localStorage.getItem(`myTeam_${question.sessionId}`);
        if (ls) return JSON.parse(ls)?.id ?? null;
      } catch { /* ignore */ }
      return null;
    };

    const checkTeamAnswer = async (): Promise<boolean> => {
      if (submissionClaimedRef.current) return true;
      const teamId = getTeamId();
      if (!teamId) return false;
      try {
        const r = await fetchWithAuth(`/api/trivia/questions/${question.questionId}/team-answer/${teamId}`);
        if (!r.ok) return false;
        const data: { answered: boolean; selectedIndex: number | null; isCorrect?: boolean; pointsAwarded?: number } = await r.json();
        if (data?.answered && data.selectedIndex != null && claimSubmission()) {
          setSelectedIndex(data.selectedIndex);
          setSent(true);
          setTeamSynced(true);
          if (timerRef.current) clearInterval(timerRef.current);
          setFeedback(data.isCorrect ? "correct" : "wrong");
          // Same as the direct broadcast handler: only drives the per-question display, the
          // running score badge comes from GameView's ranking sync exclusively.
          if (data.isCorrect && (data.pointsAwarded ?? 0) > 0) {
            setPointsAwarded(data.pointsAwarded!);
          }
          return true;
        }
      } catch { /* ignore */ }
      return false;
    };

    let closedChecksLeft = 3;
    const poll = setInterval(async () => {
      if (submissionClaimedRef.current) { clearInterval(poll); return; }
      if (questionClosed) {
        closedChecksLeft--;
        if (closedChecksLeft < 0) { clearInterval(poll); return; }
      }
      if (await checkTeamAnswer()) clearInterval(poll);
    }, 1500);

    const onVisible = () => {
      if (document.visibilityState === "visible") checkTeamAnswer();
    };
    document.addEventListener("visibilitychange", onVisible);

    return () => {
      clearInterval(poll);
      document.removeEventListener("visibilitychange", onVisible);
    };
  }, [question.questionId, question.sessionId, questionClosed, state.myTeam?.id]);

  // Auto-submit a wrong answer when timer expires so the server records the attempt
  useEffect(() => {
    if (timeLeft !== 0 || autoSubmittedRef.current) return;
    autoSubmittedRef.current = true;
    if (!claimSubmission()) return;

    const capturedTeam = myTeamRef.current;
    const capturedQuestion = question;

    const doAutoSubmit = async () => {
      try {
        const user = await userManager.getUser();
        const userId = (user?.profile?.sub as string | undefined) || "";
        const teamId = capturedTeam?.id || userId || capturedQuestion.sessionId;
        const teamName = capturedTeam?.name || user?.profile?.name || user?.profile?.preferred_username || "Participante";
        await fetchWithAuth("/api/trivia/answers", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            quizId: capturedQuestion.sessionId,
            teamId,
            teamName,
            questionId: capturedQuestion.questionId,
            answerId: "00000000-0000-0000-0000-000000000009",
            userId,
            timestamp: new Date().toISOString(),
            askedAt: capturedQuestion.askedAt,
            timeLimitSeconds: capturedQuestion.timeLimitSeconds,
          }),
        });
        setSent(true);
        setFeedback("wrong");
      } catch { /* ignore — UI already shows timeout state */ }
    };

    doAutoSubmit();
  }, [timeLeft]); // eslint-disable-line react-hooks/exhaustive-deps

  const isTimedOut = timeLeft === 0 && !sent;
  const isDisabled = sent || !!state.answersDisabled || isTimedOut || questionClosed;
  const limit = question.timeLimitSeconds || 30;
  const timerPercent = (timeLeft / limit) * 100;
  const timerColor = timeLeft <= 5 ? "#e94560" : timeLeft <= 10 ? "#fbbf24" : "#34d399";

  const handleAnswer = async (index: number) => {
    if (isDisabled) return;
    if (!claimSubmission()) return;
    setSelectedIndex(index);
    setError(null);

    if (timerRef.current) clearInterval(timerRef.current);

    try {
      const user = await userManager.getUser();
      const userId = (user?.profile?.sub as string | undefined) || "";
      const teamId = state.myTeam?.id || userId || question.sessionId;
      const teamName = state.myTeam?.name || user?.profile?.name || user?.profile?.preferred_username || "Participante";
      const payload = {
        quizId: question.sessionId,
        teamId,
        teamName,
        questionId: question.questionId,
        answerId: "00000000-0000-0000-0000-00000000000" + index,
        userId,
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
      // Immediate score update — works even when SignalR ranking sync is not available
      // (e.g. APK via tunnel). The backend also records the score independently.
      if (data.isCorrect && data.pointsAwarded > 0) {
        const newScore = (state.score || 0) + data.pointsAwarded;
        dispatch({ type: "SET_SCORE", score: newScore });
      }
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
          teamSynced
            ? <span style={{ color: "#93c5fd" }}>👥 Tu equipo ya respondió</span>
            : <span style={{ color: "#34d399" }}>Respuesta enviada</span>
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
          {pointsAwarded > 0 && <span className="points">+{pointsAwarded} pts</span>}
        </div>
      )}
      {feedback === "wrong" && (
        <div className="question-feedback wrong">
          <span>❌ Equivocada</span>
          <span className="points">0 pts</span>
        </div>
      )}
      {teamSynced && !feedback && !questionClosed && (
        <div className="question-feedback" style={{ backgroundColor: "#1a3a6a", borderColor: "#3b82f6" }}>
          <span style={{ color: "#93c5fd" }}>👥 Esperando que cierre la ronda...</span>
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
