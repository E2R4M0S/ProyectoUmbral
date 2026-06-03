import type { ReactNode } from "react";

interface ClueCardProps {
  clue: unknown;
}

interface AnswerOption {
  id: string;
  text: string;
}

function renderClueContent(clue: unknown): ReactNode {
  if (clue === null || clue === undefined) {
    return <span style={{ color: "#999", fontStyle: "italic" }}>Pista vacía</span>;
  }

  if (typeof clue === "object") {
    const obj = clue as Record<string, unknown>;

    if (typeof obj.text === "string") {
      return <p style={{ margin: 0 }}>{obj.text}</p>;
    }

    if (typeof obj.number === "number") {
      return (
        <div style={{ textAlign: "center" }}>
          <div style={{ fontSize: "2rem", fontWeight: "bold", color: "#e94560" }}>
            {obj.number}
          </div>
        </div>
      );
    }

    if (typeof obj.imageUrl === "string") {
      return (
        <img
          src={obj.imageUrl as string}
          alt="Pista"
          style={{ maxWidth: "100%", borderRadius: 4 }}
        />
      );
    }

    if (typeof obj.latitude === "number" && typeof obj.longitude === "number") {
      return (
        <div style={{ fontSize: "0.9rem", color: "#aaa" }}>
          <div>📍 Ubicación</div>
          <div style={{ fontFamily: "monospace" }}>
            {obj.latitude}, {obj.longitude}
          </div>
          {obj.hint != null && <div style={{ marginTop: 4 }}>{String(obj.hint)}</div>}
        </div>
      );
    }

    return (
      <div style={{ fontSize: "0.85rem", color: "#999" }}>
        <div style={{ marginBottom: 4 }}>📋 Datos de la pista:</div>
        <pre style={{ fontSize: "0.75rem", color: "#666", margin: 0, overflow: "auto" }}>
          {JSON.stringify(obj, null, 2)}
        </pre>
      </div>
    );
  }

  return <span style={{ color: "#999", fontStyle: "italic" }}>{String(clue)}</span>;
}

export function ClueCard({ clue }: ClueCardProps) {
  // If the clue contains answer options we'll render interactive buttons; otherwise render generic content
  const obj = typeof clue === "object" && clue !== null ? (clue as Record<string, any>) : null;

  if (obj && Array.isArray(obj.options)) {
    // Simplified: options are objects { id, text }
    const options = obj.options as AnswerOption[];
    const [selected, setSelected] = React.useState<string | null>(null);
    const [closed, setClosed] = React.useState(false);
    const [correctId, setCorrectId] = React.useState<string | null>(null);
    const [resultText, setResultText] = React.useState<string | null>(null);

    // Listen for QuestionClosed via a global hook on window (simple approach for this patch)
    React.useEffect(() => {
      function onClosed(e: any) {
        if (!e || !e.detail) return;
        try {
          const d = e.detail as { questionId: string; correctAnswerId: string };
          if (String(obj.questionId) === String(d.questionId)) {
            setCorrectId(d.correctAnswerId);
            setClosed(true);
            if (selected === d.correctAnswerId) setResultText("¡Acierto!");
            else setResultText("Error");
          }
        } catch { }
      }

      window.addEventListener("QuestionClosed", onClosed as EventListener);
      return () => window.removeEventListener("QuestionClosed", onClosed as EventListener);
    }, [obj, selected]);

    return (
      <div className="p-4 bg-slate-800 border border-slate-700 rounded-md mb-3 text-white">
        {obj.title && <div className="mb-2 font-semibold">{String(obj.title)}</div>}
        <div className="grid gap-2">
          {options.map(o => {
            const isSelected = selected === o.id;
            const isCorrect = correctId === o.id;
            const className = closed
              ? isCorrect
                ? "bg-green-600 text-white"
                : isSelected
                  ? "bg-red-600 text-white"
                  : "bg-slate-700 text-white"
              : (isSelected ? "bg-sky-600 text-white" : "bg-slate-700 text-white");

            return (
              <button key={o.id}
                className={`p-3 rounded ${className}`}
                disabled={closed}
                onClick={() => setSelected(o.id)}>
                {o.text}
              </button>
            );
          })}
        </div>
        {closed && resultText && (
          <div className="mt-3 text-lg font-bold">{resultText}</div>
        )}
      </div>
    );
  }

  return (
    <div style={{
      padding: "1rem",
      backgroundColor: "#16213e",
      border: "1px solid #0f3460",
      borderRadius: 8,
      borderLeft: "4px solid #e94560",
      color: "white",
      marginBottom: "0.75rem",
    }}>
      {renderClueContent(clue)}
    </div>
  );
}
