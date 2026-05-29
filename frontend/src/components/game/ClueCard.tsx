import type { ReactNode } from "react";

interface ClueCardProps {
  clue: unknown;
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
