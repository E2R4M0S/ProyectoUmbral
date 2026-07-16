import { Page, expect } from "@playwright/test";

const FRONTEND_URL = "http://localhost:5173";
const KEYCLOAK_URL = "http://localhost:8080";

export async function loginViaKeycloak(
  page: Page,
  username: string,
  password: string,
) {
  await page.goto(FRONTEND_URL);
  await page.waitForLoadState("networkidle");

  const loginBtn = page.locator("button", { hasText: "Iniciar Sesión" });
  if (await loginBtn.isVisible()) {
    await loginBtn.click();
  }

  await page.waitForURL(/.*\/realms\/umbral\/.*/, { timeout: 30000 });
  await page.waitForLoadState("networkidle");

  await page.fill("#username", username);
  await page.fill("#password", password);
  await page.click("#kc-login");

  await page.waitForURL(/localhost:5173/, { timeout: 30000 });
  await page.waitForLoadState("networkidle");
}

export async function registerParticipant(
  page: Page,
  data: {
    firstName: string;
    lastName: string;
    username: string;
    alias: string;
    email: string;
    password: string;
  },
) {
  await page.goto(`${FRONTEND_URL}/registro`);
  await page.waitForLoadState("networkidle");

  await page.fill("#reg-firstName", data.firstName);
  await page.fill("#reg-lastName", data.lastName);
  await page.fill("#reg-username", data.username);
  await page.fill("#reg-alias", data.alias);
  await page.fill("#reg-email", data.email);
  await page.fill("#reg-password", data.password);

  await page.click("button:has-text('Registrarse')");

  await expect(
    page.locator("text=Registro exitoso"),
  ).toBeVisible({ timeout: 15000 });
}

export function randomId(): string {
  return Math.random().toString(36).substring(2, 8);
}
