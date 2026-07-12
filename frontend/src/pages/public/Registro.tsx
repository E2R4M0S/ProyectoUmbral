import { useState } from "react";
import { RegistroForm } from "../../components/RegistroForm";
import { Link } from "react-router-dom";

export function Registro() {
  const [registered, setRegistered] = useState(false);

  function handleSuccess() {
    setRegistered(true);
  }

  if (registered) {
    return (
      <div style={{
        minHeight: "100vh", backgroundColor: "#1a1a2e", color: "white",
        display: "flex", flexDirection: "column", alignItems: "center",
        justifyContent: "center", fontFamily: "sans-serif", padding: "2rem",
        textAlign: "center"
      }}>
        <h2 style={{ color: "#e94560", marginBottom: "0.5rem" }}>Registro exitoso</h2>
        <p style={{ color: "#ccc", maxWidth: 400 }}>
          Te hemos enviado un correo de verificación. Por favor revisá tu bandeja de entrada
          y seguí el enlace para activar tu cuenta antes de iniciar sesión.
        </p>
        <Link to="/" style={{ marginTop: "1.5rem", color: "#e94560", fontWeight: 600 }}>
          Volver al inicio
        </Link>
      </div>
    );
  }

  return (
    <div style={{
      minHeight: "100vh", backgroundColor: "#1a1a2e", color: "white",
      fontFamily: "sans-serif", padding: "3rem 1rem"
    }}>
      <div style={{ maxWidth: 420, margin: "0 auto" }}>
        <h1 style={{ textAlign: "center", marginBottom: "0.5rem", fontSize: "1.8rem" }}>
          Registro de Participante
        </h1>
        <p style={{ textAlign: "center", color: "#999", marginBottom: "2rem", fontSize: 14 }}>
          Crea tu cuenta para unirte a las experiencias
        </p>
        <RegistroForm onSuccess={handleSuccess} />
        <p style={{ textAlign: "center", marginTop: "1.5rem", fontSize: 14, color: "#999" }}>
          Ya tenes cuenta?{" "}
          <Link to="/" style={{ color: "#e94560", fontWeight: 600, textDecoration: "none" }}>
            Inicia sesion
          </Link>
        </p>
      </div>
    </div>
  );
}
