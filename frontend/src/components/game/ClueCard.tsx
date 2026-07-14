import React, { type ReactNode } from "react";

interface ClueCardProps {
  clue: unknown;
}

interface AnswerOption {
  id: string;
  text: string;
}

function renderClueContent(clue: unknown): ReactNode {
  if (clue === null || clue === undefined) {
    return <span className="clue-empty">Pista vacía</span>;
  }

  if (typeof clue === "object") {
    const obj = clue as Record<string, unknown>;

    if (typeof obj.text === "string") {
      return <p>{obj.text}</p>;
    }

    if (typeof obj.number === "number") {
      return (
        <div className="clue-number">
          <span>{String(obj.number)}</span>
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
        <div className="clue-location">
          <div>📍 Ubicación</div>
          <div style={{ fontFamily: "monospace" }}>
            {obj.latitude}, {obj.longitude}
          </div>
          {obj.hint != null && <div style={{ marginTop: 4 }}>{String(obj.hint)}</div>}
        </div>
      );
    }

    return (
      <div className="clue-raw">
        <div style={{ marginBottom: 4 }}>📋 Datos de la pista:</div>
        <pre>{JSON.stringify(obj, null, 2)}</pre>
      </div>
    );
  }

  return <span className="clue-empty">{String(clue)}</span>;
}

export function ClueCard({ clue }: ClueCardProps) {
  // If the clue contains answer options we render interactive buttons
  const obj = typeof clue === "object" && clue !== null ? (clue as Record<string, any>) : null;

  if (obj && Array.isArray(obj.options)) {
    const options = obj.options as AnswerOption[];
    const [selected, setSelected] = React.useState<string | null>(null);
    const [closed, setClosed] = React.useState(false);
    const [correctId, setCorrectId] = React.useState<string | null>(null);
    const [resultText, setResultText] = React.useState<string | null>(null);

    // Listen for QuestionClosed via a global hook on window
    React.useEffect(() => {
      function onClosed(e: any) {
        if (!e || !e.detail) return;
        try {
          const d = e.detail as { questionId: string; correctAnswerId: string };
          if (obj && String(obj.questionId) === String(d.questionId)) {
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

    function btnClass(o: AnswerOption): string {
      const classes = ["clue-option-btn"];
      if (closed) {
        if (correctId === o.id) classes.push("correct");
        else if (selected === o.id) classes.push("wrong");
      } else if (selected === o.id) {
        classes.push("selected");
      }
      return classes.join(" ");
    }

    return (
      <div className="clue-options">
        {obj.title && <div className="clue-title">{String(obj.title)}</div>}
        <div className="clue-options-grid">
          {options.map((o) => (
            <button
              key={o.id}
              className={btnClass(o)}
              disabled={closed}
              onClick={() => setSelected(o.id)}
            >
              {o.text}
            </button>
          ))}
        </div>
        {closed && resultText && (
          <div className="clue-result">{resultText}</div>
        )}
      </div>
    );
  }

  return (
    <div className="clue-card">
      {renderClueContent(clue)}
    </div>
  );
}
