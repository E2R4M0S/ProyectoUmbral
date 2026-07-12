import { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'com.umbral.app',
  appName: 'UMBRAL',
  webDir: 'dist',
  server: {
    androidScheme: 'http',
    cleartext: true,
    // Allow any external URL (Keycloak IP, etc.) to stay inside the WebView.
    // Without this, Capacitor opens non-localhost URLs in Chrome, which can't
    // redirect back to https://localhost/callback after OIDC login.
    // Allow http/https URLs (Keycloak, API) to stay in the WebView.
    // umbral:// is intentionally excluded so Android routes it as an intent (deep link).
    allowNavigation: ['http://*', 'https://*'],
    // Para live reload (dev): descomentar la línea de abajo y rebuildar APK una vez
    // url: 'http://192.168.0.2:5173',
  },
  android: {
    // The WebView runs at https://localhost. Without this, Android blocks HTTP
    // navigation to the backend/Keycloak (mixed content from HTTPS to HTTP).
    allowMixedContent: true,
  },
  plugins: {
    BarcodeScanner: {
      // Permissions are declared in AndroidManifest.xml
    },
  },
};

export default config;
