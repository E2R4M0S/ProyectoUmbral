import { useState } from "react";
import { RegistroForm } from "../../components/RegistroForm";
import { userManager } from "../../auth/keycloak";

export function Registro() {
  const [registered, setRegistered] = useState(false);

  function handleSuccess() {
    setRegistered(true);
    // Iniciar flujo OIDC inmediatamente después del registro exitoso.
    // El usuario será redirigido a Keycloak y podrá ingresar sus credenciales.
    userManager.signinRedirect({
      extraQueryParams: { audience: "umbral-gateway" },
    });
  }

  if (registered) {
    return (
      <div style={{ textAlign: "center", padding: "2rem" }}>
        <h2>¡Registro exitoso!</h2>
        <p>Redirigiendo al inicio de sesión...</p>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: 500, margin: "0 auto", padding: "2rem" }}>
      <h1 style={{ textAlign: "center", marginBottom: "1.5rem" }}>
        Registro de Participante
      </h1>
      <RegistroForm onSuccess={handleSuccess} />
      <p style={{ textAlign: "center", marginTop: "1.25rem", fontSize: 14 }}>
        ¿Ya tenés cuenta?{" "}
        <a
          href="/login"
          onClick={(e) => {
            e.preventDefault();
            userManager.signinRedirect({
              extraQueryParams: { audience: "umbral-gateway" },
            });
          }}
        >
          Iniciá sesión
        </a>
      </p>
    </div>
  );
}
