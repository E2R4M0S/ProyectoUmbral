import { useState } from "react";
import { RegistroForm } from "../../components/RegistroForm";
import { Link } from "react-router-dom";

export function Registro() {
  const [registered, setRegistered] = useState(false);

  if (registered) {
    return (
      <div className="registro-success">
        <h2>Registro exitoso</h2>
        <p>Ya puedes iniciar sesión.</p>
        <Link to="/">Volver al inicio</Link>
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
