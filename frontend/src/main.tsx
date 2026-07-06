import { createRoot } from "react-dom/client";
import { AuthProvider } from "./auth/AuthProvider";
import App from "./App";
import "./styles/global.css";

const rootElement = document.getElementById("root");
if (!rootElement) throw new Error("Root element not found");

createRoot(rootElement).render(
    <AuthProvider>
      <App />
    </AuthProvider>,
);
