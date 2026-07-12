import { useNavigate, useParams } from "react-router-dom";

export function MisionCompletada() {
  const navigate = useNavigate();
  const { sessionId } = useParams<{ sessionId: string }>();

  function handleReturn() {
    if (sessionId) sessionStorage.removeItem(`joined_${sessionId}`);
    navigate("/", { replace: true });
  }

  return (
    <div style={containerStyle}>
      <div style={iconStyle}>🏆</div>
      <h2 style={titleStyle}>¡Misión Completada!</h2>
      <p style={subtitleStyle}>
        Encontraste todas las ubicaciones. ¡Excelente trabajo!
      </p>
      <button onClick={handleReturn} style={btnStyle}>
        Volver al inicio
      </button>
    </div>
  );
}

const containerStyle: React.CSSProperties = {
  maxWidth: 400,
  margin: "0 auto",
  padding: "3rem 1rem",
  textAlign: "center",
  color: "white",
};

const iconStyle: React.CSSProperties = {
  fontSize: 72,
  marginBottom: "1rem",
};

const titleStyle: React.CSSProperties = {
  fontSize: "1.75rem",
  color: "#e94560",
  marginBottom: "0.75rem",
};

const subtitleStyle: React.CSSProperties = {
  color: "#999",
  fontSize: 15,
  marginBottom: "2rem",
  lineHeight: 1.5,
};

const btnStyle: React.CSSProperties = {
  padding: "12px 32px",
  backgroundColor: "#e94560",
  color: "#fff",
  border: "none",
  borderRadius: 8,
  cursor: "pointer",
  fontSize: 16,
  fontWeight: 600,
};
