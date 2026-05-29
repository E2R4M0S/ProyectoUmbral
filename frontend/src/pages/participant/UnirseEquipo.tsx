import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { joinTeam, ApiError } from "../../services/teamsApi";

function validateJoinCode(value: string): string | undefined {
  if (!value.trim()) return "El código de unión es obligatorio.";
  if (value.trim().length !== 6) return "El código debe tener 6 caracteres.";
  return undefined;
}

const inputStyle = (hasError: boolean): React.CSSProperties => ({
  width: "100%",
  padding: 12,
  border: hasError ? "2px solid #e94560" : "1px solid #0f3460",
  borderRadius: 8,
  boxSizing: "border-box",
  backgroundColor: "#16213e",
  color: "white",
  fontSize: 24,
  textAlign: "center",
  letterSpacing: 8,
  textTransform: "uppercase",
});

const labelStyle: React.CSSProperties = {
  display: "block",
  marginBottom: 4,
  fontWeight: 600,
};

const errorStyle: React.CSSProperties = {
  color: "#e94560",
  fontSize: 12,
  margin: "4px 0 0",
};

const fieldGroupStyle: React.CSSProperties = {
  marginBottom: 14,
};

const submitBtnStyle = (disabled: boolean): React.CSSProperties => ({
  width: "100%",
  padding: 10,
  backgroundColor: disabled ? "#999" : "#0f3460",
  color: "#fff",
  border: "none",
  borderRadius: 4,
  cursor: disabled ? "not-allowed" : "pointer",
  fontSize: 16,
  fontWeight: 600,
});

const containerStyle: React.CSSProperties = {
  maxWidth: 400,
  margin: "0 auto",
  padding: "1rem",
};

const successStyle: React.CSSProperties = {
  marginBottom: 16,
  padding: "12px 16px",
  border: "1px solid #28a745",
  borderRadius: 4,
  backgroundColor: "#1a4d1a",
  color: "#28a745",
};

const errorMsgStyle: React.CSSProperties = {
  color: "#e94560",
  marginBottom: 16,
  padding: "8px 12px",
  border: "1px solid #e94560",
  borderRadius: 4,
  backgroundColor: "#2d1a1a",
};

export function UnirseEquipo() {
  const navigate = useNavigate();
  const [joinCode, setJoinCode] = useState("");
  const [codeError, setCodeError] = useState<string>();
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [success, setSuccess] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitError(null);
    setSuccess(false);

    const error = validateJoinCode(joinCode);
    if (error) { setCodeError(error); return; }
    setCodeError(undefined);

    setIsSubmitting(true);
    try {
      await joinTeam(joinCode.trim().toUpperCase());
      setSuccess(true);
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 403) setSubmitError("Código inválido. Verificá el código.");
        else if (err.status === 409) setSubmitError("Ya estás en este equipo.");
        else setSubmitError("Error al unirse. Intentalo de nuevo.");
      } else {
        setSubmitError("Error de conexión.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div style={{ maxWidth: 400, margin: "0 auto", padding: "2rem 1rem", textAlign: "center" }}>
      <h2 style={{ marginBottom: "0.5rem" }}>Unirse a un Equipo</h2>
      <p style={{ color: "#999", marginBottom: "2rem", fontSize: 14 }}>
        Ingresá el código de 6 caracteres que te compartió el líder
      </p>

      {success && (
        <div style={{ marginBottom: 16, padding: 16, border: "1px solid #28a745", borderRadius: 8, backgroundColor: "#1a4d1a", color: "#28a745" }}>
          ¡Te uniste al equipo correctamente!
        </div>
      )}
      {submitError && (
        <div style={{ marginBottom: 16, padding: 12, border: "1px solid #e94560", borderRadius: 8, backgroundColor: "#2d1a1a", color: "#e94560" }}>
          {submitError}
        </div>
      )}

      <form onSubmit={handleSubmit}>
        <input
          type="text"
          maxLength={6}
          placeholder="ABC123"
          value={joinCode}
          onChange={(e) => setJoinCode(e.target.value.toUpperCase())}
          style={inputStyle(Boolean(codeError))}
          autoFocus
        />
        {codeError && <p style={{ color: "#e94560", fontSize: 12, margin: "4px 0 0" }}>{codeError}</p>}
        <button type="submit" disabled={isSubmitting} style={{
          width: "100%", padding: 12, marginTop: 16,
          backgroundColor: isSubmitting ? "#999" : "#e94560",
          color: "white", border: "none", borderRadius: 8,
          cursor: isSubmitting ? "not-allowed" : "pointer",
          fontSize: 16, fontWeight: 600
        }}>
          {isSubmitting ? "Uniéndose..." : "Unirse al Equipo"}
        </button>
      </form>
    </div>
  );
}
