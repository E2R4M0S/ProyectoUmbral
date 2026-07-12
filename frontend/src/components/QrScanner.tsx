import { useEffect, useRef, useState } from "react";
import { Html5Qrcode } from "html5-qrcode";
import { BarcodeScanner } from "@capacitor-community/barcode-scanner";
import { Capacitor } from "@capacitor/core";

interface QrScannerProps {
  active: boolean;
  onScan: (text: string) => void;
}

const WEB_READER_ID = "qr-reader";
const FILE_READER_ID = "qr-file-reader";

export function QrScanner({ active, onScan }: QrScannerProps) {
  const isNative = Capacitor.isNativePlatform();
  const isSecure = window.isSecureContext;

  const [cameraError, setCameraError] = useState<string | null>(null);
  const [decoding, setDecoding] = useState(false);
  const [photoError, setPhotoError] = useState<string | null>(null);
  const onScanRef = useRef(onScan);
  onScanRef.current = onScan;

  // ── Native BarcodeScanner (APK — siempre, live reload o producción) ───────
  // Usa el plugin nativo para dar experiencia de escáner continuo (estilo WhatsApp).
  useEffect(() => {
    if (!active || !isNative) return;

    let stopped = false;

    async function startNative() {
      const permission = await BarcodeScanner.checkPermission({ force: true });
      // In barcode-scanner v4, `granted` can be undefined on first ask (the OS
      // dialog is shown and the promise resolves before the user responds).
      // `denied` being explicitly true is the only reliable "blocked" signal.
      if (permission.denied) {
        setCameraError("Permiso de cámara denegado. Habilitalo en Ajustes → Aplicaciones → UMBRAL → Permisos.");
        return;
      }
      document.body.classList.add("scanner-active");
      BarcodeScanner.hideBackground();
      const result = await BarcodeScanner.startScan();
      if (!stopped && result.hasContent) {
        onScanRef.current(result.content);
      }
    }

    startNative().catch(() => {
      if (!stopped) setCameraError("No se pudo iniciar el escáner.");
    });

    return () => {
      stopped = true;
      BarcodeScanner.showBackground();
      BarcodeScanner.stopScan();
      document.body.classList.remove("scanner-active");
    };
  }, [active, isNative]);

  // ── Live camera web (intenta siempre cuando no es nativo) ────────────────
  // Android WebView permite getUserMedia sobre HTTP, así que no chequeamos isSecure.
  useEffect(() => {
    if (!active || isNative) return;

    const scanner = new Html5Qrcode(WEB_READER_ID);
    setCameraError(null);

    scanner
      .start(
        { facingMode: "environment" },
        { fps: 15, qrbox: { width: 250, height: 250 } },
        (text) => { onScanRef.current(text); },
        undefined,
      )
      .catch((err: unknown) => {
        const name = (err as DOMException)?.name ?? "";
        if (name === "NotFoundError" || name === "DevicesNotFoundError") {
          setCameraError("No se encontró ninguna cámara trasera.");
        } else if (name === "NotAllowedError" || name === "PermissionDeniedError") {
          setCameraError("Permiso de cámara denegado. Habilitalo en Ajustes → Aplicaciones → UMBRAL → Permisos.");
        } else {
          setCameraError("No se pudo iniciar la cámara.");
        }
      });

    return () => {
      scanner.stop().catch(() => {}).finally(() => scanner.clear());
    };
  }, [active, isNative]);

  // ── Photo fallback (navegador sobre HTTP) ─────────────────────────────────
  async function handleFileInput(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;
    setDecoding(true);
    setPhotoError(null);
    try {
      const scanner = new Html5Qrcode(FILE_READER_ID);
      const text = await scanner.scanFile(file, false);
      scanner.clear();
      onScanRef.current(text);
    } catch {
      setPhotoError("No se pudo leer el QR. Tomá una foto más clara y cercana al código.");
    } finally {
      setDecoding(false);
      e.target.value = "";
    }
  }

  // ── Native overlay ────────────────────────────────────────────────────────
  if (isNative) {
    if (cameraError) return <ErrorBox message={cameraError} />;
    return (
      <div className="scanner-native-overlay">
        <div className="scanner-native-frame"><span /></div>
        <p className="scanner-native-label">Apuntá la cámara al código QR</p>
      </div>
    );
  }

  // ── Error: cámara no disponible → fallback foto ───────────────────────────
  if (cameraError) {
    return (
      <div style={fallbackStyle}>
        <div id={FILE_READER_ID} style={{ display: "none" }} />
        <p style={{ color: "#e94560", fontSize: 13, marginBottom: "1rem" }}>{cameraError}</p>
        <p style={{ color: "#aaa", fontSize: 14, marginBottom: "1.25rem", lineHeight: 1.5 }}>
          Apuntá la cámara al código QR y tomá la foto.
        </p>
        <label style={photoButtonStyle}>
          {decoding ? "Leyendo QR..." : "📷 Abrir cámara"}
          <input
            type="file"
            accept="image/*"
            capture="environment"
            style={{ display: "none" }}
            onChange={handleFileInput}
            disabled={decoding}
          />
        </label>
        {photoError && (
          <p style={{ color: "#e94560", fontSize: 13, marginTop: "1rem" }}>{photoError}</p>
        )}
      </div>
    );
  }

  // ── Visor de cámara continuo ──────────────────────────────────────────────
  return (
    <div style={wrapperStyle}>
      <div id={WEB_READER_ID} style={readerStyle} />
    </div>
  );
}

function ErrorBox({ message }: { message: string }) {
  return (
    <div style={errorContainerStyle}>
      <p style={{ color: "#e94560", fontWeight: 700, marginBottom: 8 }}>Sin acceso a la cámara</p>
      <p style={{ color: "#999", fontSize: 13 }}>{message}</p>
    </div>
  );
}

const wrapperStyle: React.CSSProperties = {
  display: "flex",
  justifyContent: "center",
  width: "100%",
};

const readerStyle: React.CSSProperties = {
  width: "100%",
  maxWidth: 360,
  borderRadius: 12,
  overflow: "hidden",
  border: "2px solid #0f3460",
};

const fallbackStyle: React.CSSProperties = {
  textAlign: "center",
  padding: "1.5rem",
  border: "1px solid #0f3460",
  borderRadius: 10,
  backgroundColor: "#0d1b35",
};

const photoButtonStyle: React.CSSProperties = {
  display: "inline-block",
  padding: "14px 32px",
  backgroundColor: "#e94560",
  color: "white",
  borderRadius: 8,
  cursor: "pointer",
  fontSize: "1rem",
  fontWeight: 700,
};

const errorContainerStyle: React.CSSProperties = {
  textAlign: "center",
  padding: "1.5rem",
  border: "1px solid #e94560",
  borderRadius: 8,
  backgroundColor: "#2d1a1a",
};
