import { useState } from "react";
import { RegistroForm } from "../../components/RegistroForm";
import { Link } from "react-router-dom";

export function Registro() {
  const [registered, setRegistered] = useState(false);

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
          Tu cuenta fue creada correctamente. Ya podés iniciar sesión con tu usuario y contraseña.
        </p>
        <Link to="/" style={{ marginTop: "1.5rem", color: "#e94560", fontWeight: 600 }}>
          Ir al inicio de sesión
        </Link>
      </div>
    );
  }

  return (
    <div className="registro-page">
      <div className="registro-page-inner">
        <h1>Registro de Participante</h1>
        <p className="page-subtitle">Crea tu cuenta para unirte a las experiencias</p>
        <RegistroForm onSuccess={() => setRegistered(true)} />
        <p style={{ textAlign: "center", marginTop: "1.5rem", fontSize: "0.875rem", color: "var(--text-muted)" }}>
          ¿Ya tienes cuenta?{" "}
          <Link to="/" style={{ color: "var(--accent)", fontWeight: 600, textDecoration: "none" }}>
            Inicia sesión
          </Link>
        </p>
      </div>
    </div>
  );
}
