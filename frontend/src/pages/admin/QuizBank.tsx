import { useState, useEffect, type FormEvent } from "react";
import { fetchWithAuth } from "../../services/api";

interface Quiz {
  id: string;
  title: string;
  questionCount: number;
}

const s: Record<string, React.CSSProperties> = {
  container: { maxWidth: 700, margin: "0 auto", color: "white", fontFamily: "sans-serif", padding: "1rem" },
  title: { fontSize: "1.5rem", color: "#e94560", margin: "0 0 1rem" },
  card: { padding: "1rem", backgroundColor: "#16213e", borderRadius: 8, border: "1px solid #0f3460", marginBottom: "0.75rem" },
  btn: (bg: string): React.CSSProperties => ({ padding: "8px 16px", backgroundColor: bg, color: "white", border: "none", borderRadius: 6, cursor: "pointer", fontSize: 14, fontWeight: 600 }),
  input: { width: "100%", padding: "8px", borderRadius: 4, border: "1px solid #0f3460", backgroundColor: "#16213e", color: "white", fontSize: "0.9rem", marginBottom: "0.5rem", boxSizing: "border-box" as const },
  label: { display: "block", marginBottom: 4, fontSize: 13, color: "#ccc" },
};

export function QuizBank() {
  const [quizzes, setQuizzes] = useState<Quiz[]>([]);
  const [title, setTitle] = useState("");
  const [questions, setQuestions] = useState([{ text: "", answers: [{ text: "", isCorrect: false }, { text: "", isCorrect: false }] }]);
  const [saving, setSaving] = useState(false);
  const [msg, setMsg] = useState("");

  useEffect(() => {
    fetchWithAuth("/api/quizzes").then(r => r.json()).then(setQuizzes).catch(() => {});
  }, []);

  const addQuestion = () => {
    setQuestions([...questions, { text: "", answers: [{ text: "", isCorrect: false }, { text: "", isCorrect: false }] }]);
  };

  const updateQuestion = (i: number, text: string) => {
    const qs = [...questions];
    qs[i] = { ...qs[i], text };
    setQuestions(qs);
  };

  const updateAnswer = (qi: number, ai: number, text: string) => {
    const qs = [...questions];
    qs[qi].answers[ai] = { ...qs[qi].answers[ai], text };
    setQuestions(qs);
  };

  const markCorrect = (qi: number, ai: number) => {
    const qs = [...questions];
    qs[qi].answers = qs[qi].answers.map((a, i) => ({ ...a, isCorrect: i === ai }));
    setQuestions(qs);
  };

  const addAnswer = (qi: number) => {
    const qs = [...questions];
    qs[qi].answers.push({ text: "", isCorrect: false });
    setQuestions(qs);
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!title.trim() || questions.some(q => !q.text.trim() || q.answers.some(a => !a.text.trim()))) {
      setMsg("Completá el título, todas las preguntas y respuestas");
      return;
    }
    setSaving(true);
    setMsg("");
    try {
      const resp = await fetchWithAuth("/api/quizzes", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ title: title.trim(), questions }),
      });
      if (!resp.ok) throw new Error(await resp.text());
      const result = await resp.json();
      setMsg(`Quiz creado: ${result.id}`);
      setTitle("");
      setQuestions([{ text: "", answers: [{ text: "", isCorrect: false }, { text: "", isCorrect: false }] }]);
      // Refresh list
      const list = await fetchWithAuth("/api/quizzes").then(r => r.json());
      setQuizzes(list);
    } catch (ex: any) {
      setMsg("Error: " + (ex?.message || "desconocido"));
    } finally {
      setSaving(false);
    }
  };

  return (
    <div style={s.container}>
      <h2 style={s.title}>Banco de Preguntas</h2>

      <div style={s.card}>
        <h3 style={{ color: "#e94560", margin: "0 0 0.75rem", fontSize: 16 }}>Crear Quiz</h3>
        <form onSubmit={handleSubmit}>
          <input placeholder="Título del quiz" value={title} onChange={e => setTitle(e.target.value)} style={s.input} />

          {questions.map((q, qi) => (
            <div key={qi} style={{ border: "1px solid #0f3460", borderRadius: 6, padding: "0.75rem", marginBottom: "0.75rem" }}>
              <div style={s.label}>Pregunta {qi + 1}</div>
              <input placeholder="Texto de la pregunta" value={q.text} onChange={e => updateQuestion(qi, e.target.value)} style={s.input} />

              {q.answers.map((a, ai) => (
                <div key={ai} style={{ display: "flex", gap: 8, alignItems: "center", marginBottom: 4 }}>
                  <input
                    placeholder={`Opción ${ai + 1}`}
                    value={a.text}
                    onChange={e => updateAnswer(qi, ai, e.target.value)}
                    style={{ ...s.input, marginBottom: 0, flex: 1 }}
                  />
                  <label style={{ color: "#ccc", fontSize: 12, cursor: "pointer", whiteSpace: "nowrap" }}>
                    <input type="radio" name={`correct-${qi}`} checked={a.isCorrect} onChange={() => markCorrect(qi, ai)} />
                    {" Correcta"}
                  </label>
                </div>
              ))}
              <button type="button" onClick={() => addAnswer(qi)} style={{ ...s.btn("#0f3460"), fontSize: 12, marginTop: 4 }}>+ Agregar opción</button>
            </div>
          ))}

          <button type="button" onClick={addQuestion} style={{ ...s.btn("#0f3460"), marginBottom: "0.75rem" }}>+ Agregar pregunta</button>
          <br />
          <button type="submit" disabled={saving} style={s.btn(saving ? "#999" : "#e94560")}>
            {saving ? "Guardando..." : "Guardar Quiz"}
          </button>
        </form>
        {msg && <p style={{ marginTop: "0.5rem", color: msg.includes("Error") ? "#e94560" : "#28a745", fontSize: "0.85rem" }}>{msg}</p>}
      </div>

      <h3 style={{ color: "#e94560", margin: "1.5rem 0 0.75rem", fontSize: 16 }}>Quizzes Existentes</h3>
      {quizzes.length === 0 ? (
        <p style={{ color: "#999" }}>No hay quizzes creados</p>
      ) : (
        quizzes.map(q => (
          <div key={q.id} style={s.card}>
            <div style={{ fontWeight: 600 }}>{q.title}</div>
            <div style={{ color: "#999", fontSize: "0.85rem" }}>{q.questionCount} preguntas</div>
          </div>
        ))
      )}
    </div>
  );
}
