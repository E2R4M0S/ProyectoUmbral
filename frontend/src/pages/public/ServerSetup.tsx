import { useState } from "react";
import { setServerHost, getServerHost, clearServerHost } from "../../config/serverConfig";

export function ServerSetup() {
  const [host, setHost] = useState(getServerHost());
  const [error, setError] = useState("");

  function handleSave() {
    const value = host.trim();
    if (!value) {
      setError("Ingresa la IP o dirección del servidor.");
      return;
    }
    setServerHost(value);
    window.location.href = "/";
  }

  function handleReset() {
    clearServerHost();
    setHost("");
    setError("");
  }

  return (
    <div style={containerStyle}>
      <div style={cardStyle}>
        <div style={logoStyle}>U</div>
        <h1 style={titleStyle}>UMBRAL</h1>
        <p style={subtitleStyle}>Configuración del servidor</p>

        <div style={fieldStyle}>
          <label style={labelStyle}>IP o dirección del servidor</label>
          <input
            style={inputStyle}
            type="text"
            placeholder="ej: 192.168.0.10 o https://xxxx.trycloudflare.com"
            value={host}
            onChange={(e) => { setHost(e.target.value); setError(""); }}
            onKeyDown={(e) => e.key === "Enter" && handleSave()}
            autoCapitalize="none"
            autoCorrect="off"
            spellCheck={false}
          />
          {error && <p style={errorStyle}>{error}</p>}
        </div>

        <p style={hintStyle}>
          Misma red Wi-Fi que la PC: ingresá su IP (verla con{" "}
          <strong>ipconfig</strong> en la PC).{"\n"}
          Desde otra red (ej. el profesor evaluando): ingresá la URL completa
          del túnel (https://...) que te dio Cloudflare.
        </p>

        <button style={btnStyle} onClick={handleSave}>
          Guardar y continuar
        </button>

        {getServerHost() && (
          <button style={resetBtnStyle} onClick={handleReset}>
            Limpiar configuración
          </button>
        )}
      </div>
    </div>
  );
}

const containerStyle: React.CSSProperties = {
  minHeight: "100vh",
  display: "flex",
  alignItems: "center",
  justifyContent: "center",
  backgroundColor: "#0a0f1e",
  padding: "1rem",
};

const cardStyle: React.CSSProperties = {
  width: "100%",
  maxWidth: 380,
  backgroundColor: "#0d1b35",
  border: "1px solid #0f3460",
  borderRadius: 16,
  padding: "2rem 1.5rem",
  textAlign: "center",
};

const logoStyle: React.CSSProperties = {
  width: 56,
  height: 56,
  borderRadius: "50%",
  backgroundColor: "#e94560",
  color: "white",
  fontSize: 28,
  fontWeight: 700,
  display: "flex",
  alignItems: "center",
  justifyContent: "center",
  margin: "0 auto 1rem",
};

const titleStyle: React.CSSProperties = {
  color: "white",
  fontSize: "1.5rem",
  fontWeight: 700,
  margin: 0,
};

const subtitleStyle: React.CSSProperties = {
  color: "#aaa",
  fontSize: 14,
  marginTop: 4,
  marginBottom: "1.5rem",
};

const fieldStyle: React.CSSProperties = {
  marginBottom: "1rem",
  textAlign: "left",
};

const labelStyle: React.CSSProperties = {
  display: "block",
  color: "#ccc",
  fontSize: 13,
  marginBottom: 6,
};

const inputStyle: React.CSSProperties = {
  width: "100%",
  padding: "12px 14px",
  backgroundColor: "#0a0f1e",
  border: "1px solid #0f3460",
  borderRadius: 8,
  color: "white",
  fontSize: 16,
  boxSizing: "border-box",
};

const errorStyle: React.CSSProperties = {
  color: "#e94560",
  fontSize: 13,
  marginTop: 6,
};

const hintStyle: React.CSSProperties = {
  color: "#666",
  fontSize: 12,
  lineHeight: 1.6,
  marginBottom: "1.5rem",
  whiteSpace: "pre-line",
};

const btnStyle: React.CSSProperties = {
  width: "100%",
  padding: "14px",
  backgroundColor: "#e94560",
  color: "white",
  border: "none",
  borderRadius: 8,
  fontSize: 16,
  fontWeight: 700,
  cursor: "pointer",
};

const resetBtnStyle: React.CSSProperties = {
  width: "100%",
  padding: "10px",
  backgroundColor: "transparent",
  color: "#666",
  border: "1px solid #1a2a4a",
  borderRadius: 8,
  fontSize: 13,
  cursor: "pointer",
  marginTop: "0.75rem",
};
