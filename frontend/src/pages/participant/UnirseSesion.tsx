import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { joinSession, ApiError } from "../../services/sessionsApi";

function validatePin(value: string): string | undefined {
  if (!value.trim()) return "El PIN es obligatorio.";
  if (value.trim().length !== 6) return "El PIN debe tener 6 caracteres.";
  return undefined;
}

const containerStyle: React.CSSProperties = {
  maxWidth: 400,
  margin: "0 auto",
  padding: "2rem 1rem",
  textAlign: "center",
};

const inputStyle = (hasError: boolean): React.CSSProperties => ({
  width: "100%",
  padding: 12,
  border: hasError ? "2px solid #e94560" : "2px solid #0f3460",
  borderRadius: 8,
  boxSizing: "border-box",
  backgroundColor: "#16213e",
  color: "white",
  fontSize: 24,
  textAlign: "center",
  letterSpacing: 8,
  textTransform: "uppercase",
  outline: "none",
});

const labelStyle: React.CSSProperties = {
  display: "block",
  marginBottom: 4,
  fontWeight: 600,
  color: "#ccc",
};

const errorStyle: React.CSSProperties = {
  color: "#e94560",
  fontSize: 12,
  margin: "4px 0 0",
  textAlign: "left",
};

const submitBtnStyle = (disabled: boolean): React.CSSProperties => ({
  width: "100%",
  padding: 12,
  marginTop: 16,
  backgroundColor: disabled ? "#999" : "#e94560",
  color: "#fff",
  border: "none",
  borderRadius: 8,
  cursor: disabled ? "not-allowed" : "pointer",
  fontSize: 16,
  fontWeight: 600,
});

const errorMsgStyle: React.CSSProperties = {
  marginBottom: 16,
  padding: "12px 16px",
  border: "1px solid #e94560",
  borderRadius: 8,
  backgroundColor: "#2d1a1a",
  color: "#e94560",
  fontSize: 14,
};

export function UnirseSesion() {
  const navigate = useNavigate();
  const [pin, setPin] = useState("");
  const [pinError, setPinError] = useState<string>();
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitError(null);

    const error = validatePin(pin);
    if (error) { setPinError(error); return; }
    setPinError(undefined);

    setIsSubmitting(true);
    try {
      const result = await joinSession(pin.trim().toUpperCase());
      sessionStorage.setItem(`joined_${result.sessionId}`, "1");
      navigate(`/juego/${result.sessionId}`);
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 404) setSubmitError("PIN inválido. Verificá el código.");
        else setSubmitError("Error al unirse. Intentalo de nuevo.");
      } else {
        setSubmitError("Error de conexión.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div style={containerStyle}>
      <h2 style={{ marginBottom: "0.5rem", color: "white" }}>Unirse a Sesión</h2>
      <p style={{ color: "#999", marginBottom: "2rem", fontSize: 14 }}>
        Ingresá el PIN que te compartió el host para unirte a la experiencia
      </p>

      {submitError && (
        <div style={errorMsgStyle}>{submitError}</div>
      )}

      <form onSubmit={handleSubmit}>
        <label htmlFor="pin" style={labelStyle}>Código PIN</label>
        <input
          id="pin"
          type="text"
          maxLength={6}
          placeholder="ABC123"
          value={pin}
          onChange={(e) => setPin(e.target.value.toUpperCase())}
          style={inputStyle(Boolean(pinError))}
          autoFocus
        />
        {pinError && <p style={errorStyle}>{pinError}</p>}

        <button
          type="submit"
          disabled={isSubmitting}
          style={submitBtnStyle(isSubmitting)}
        >
          {isSubmitting ? "Uniéndose..." : "Entrar a la Sesión"}
        </button>
      </form>
    </div>
  );
}
