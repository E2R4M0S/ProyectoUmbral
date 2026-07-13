#!/bin/bash
# keycloak-setup.sh — Configura SMTP, Required Actions y Default Actions en Keycloak
# Se ejecuta UNA SOLA VEZ después de que Keycloak arranca.
# Idempotente: se puede ejecutar siempre, no duplica config.

set -euo pipefail

BASE="http://keycloak:8080"
REALM="umbral"
ADMIN="admin"
ADMIN_PASS="admin123"

echo "⏳ Esperando a Keycloak..."
for i in $(seq 1 30); do
  if curl -s -o /dev/null -w "%{http_code}" "$BASE/realms/$REALM" 2>/dev/null | grep -q 200; then
    echo "✅ Keycloak listo"
    break
  fi
  sleep 2
done

echo "🔑 Obteniendo token de admin..."
TOKEN=$(curl -s -X POST "$BASE/realms/master/protocol/openid-connect/token" \
  -d "client_id=admin-cli&username=$ADMIN&password=$ADMIN_PASS&grant_type=password" | \
  python3 -c "import sys,json; print(json.load(sys.stdin)['access_token'])" 2>/dev/null || \
  python -c "import sys,json; print(json.load(sys.stdin)['access_token'])"
)

echo "📧 Configurando SMTP server..."
curl -s -X PUT "$BASE/admin/realms/$REALM" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "smtpServer": {
      "host": "mailhog",
      "port": "1025",
      "from": "noreply@umbral.local",
      "auth": "false",
      "ssl": "false",
      "starttls": "false"
    }
  }' > /dev/null && echo "   ✅ SMTP configurado (mailhog:1025)"

echo "🔧 Configurando VERIFY_EMAIL como default action..."
curl -s -X PUT "$BASE/admin/realms/$REALM/authentication/required-actions/VERIFY_EMAIL" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "alias": "VERIFY_EMAIL",
    "name": "Verify Email",
    "providerId": "VERIFY_EMAIL",
    "enabled": true,
    "defaultAction": true,
    "priority": 50,
    "config": {}
  }' > /dev/null && echo "   ✅ VERIFY_EMAIL = enabled + defaultAction"

echo "🔧 Asegurando UPDATE_PASSWORD habilitado..."
curl -s -X PUT "$BASE/admin/realms/$REALM/authentication/required-actions/UPDATE_PASSWORD" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "alias": "UPDATE_PASSWORD",
    "name": "Update Password",
    "providerId": "UPDATE_PASSWORD",
    "enabled": true,
    "defaultAction": false,
    "priority": 40,
    "config": {}
  }' > /dev/null && echo "   ✅ UPDATE_PASSWORD = enabled"

echo ""
echo "========================================="
echo "🎉 Keycloak setup completado con éxito"
echo "========================================="
echo "📬 MailHog UI: http://localhost:8025"
echo "🔑 Keycloak:   http://localhost:8080"
echo "========================================="