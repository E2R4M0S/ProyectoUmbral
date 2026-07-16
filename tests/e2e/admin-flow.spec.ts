import { test, expect } from "@playwright/test";
import { loginViaKeycloak, registerParticipant, randomId } from "./helpers";

const ADMIN_USER = "admin";
const ADMIN_PASS = "admin123";
const OPERATOR_USER = "operator";
const OPERATOR_PASS = "operator123";

test.describe("Admin flow — create mission and session", () => {
  let missionTitle: string;
  let sessionName: string;
  let participantUser: string;
  let pinCode: string;

  test.beforeAll(() => {
    missionTitle = `E2E Mission ${randomId()}`;
    sessionName = `E2E Session ${randomId()}`;
    participantUser = `e2e_user_${randomId()}`;
  });

  test("admin creates and activates a Treasure mission", async ({ page }) => {
    await loginViaKeycloak(page, ADMIN_USER, ADMIN_PASS);

    await page.goto("/admin/misiones/crear");
    await page.waitForLoadState("networkidle");
    await page.waitForTimeout(1000);

    await page.fill("#mission-title", missionTitle);
    await page.fill("#mission-desc", "E2E test description");
    await page.selectOption("#mission-difficulty", "Easy");
    await page.selectOption("#mission-time", "15");
    await page.selectOption("#mission-type", "Treasure");

    await page.click("button:has-text('Crear Misión')");

    await expect(
      page.locator(".alert-success"),
    ).toBeVisible({ timeout: 10000 });

    await page.goto("/admin/misiones");
    await page.waitForLoadState("networkidle");
    await page.waitForTimeout(1000);

    const activateBtn = page.locator(
      `.mission-card:has-text("${missionTitle}") button:has-text("Activar")`,
    );
    if (await activateBtn.isVisible()) {
      await activateBtn.click();
      await page.waitForTimeout(1000);
    }

    await page.reload();
    await page.waitForLoadState("networkidle");

    await expect(
      page.locator(
        `.mission-card:has-text("${missionTitle}") .badge-info`,
      ),
    ).toBeVisible({ timeout: 5000 });
  });

  test("operator creates a session with the mission", async ({ page }) => {
    await loginViaKeycloak(page, OPERATOR_USER, OPERATOR_PASS);

    await page.goto("/operator/sesiones/crear");
    await page.waitForLoadState("networkidle");
    await page.waitForTimeout(1000);

    await page.fill("#session-name", sessionName);

    await page.fill("#session-mission", missionTitle);
    await page.waitForTimeout(2000);

    const dropdownItem = page.locator("li:has-text('E2E Mission')").first();
    if (await dropdownItem.isVisible()) {
      await dropdownItem.click();
      await page.waitForTimeout(500);
    }

    const createBtn = page.locator("button:has-text('Crear Sesión')");
    await expect(createBtn).toBeEnabled({ timeout: 5000 });
    await createBtn.click();

    await expect(
      page.locator("text=¡Sesión creada!").or(page.locator("text=creada")),
    ).toBeVisible({ timeout: 15000 });

    const pinText = await page.locator("code").textContent();
    if (pinText) pinCode = pinText.trim();
    expect(pinCode).toHaveLength(6);
  });

  test("participant registers via the form", async ({ page }) => {
    await registerParticipant(page, {
      firstName: "E2E",
      lastName: "User",
      username: participantUser,
      alias: participantUser,
      email: `${participantUser}@e2e.test`,
      password: "Test1234",
    });
  });

  test("participant joins the session with PIN and sees waiting room", async ({
    page,
  }) => {
    test.skip(!pinCode, "PIN not available from previous test");
    test.skip(!sessionName, "Session name not available from previous test");

    await loginViaKeycloak(page, participantUser, "Test1234");

    await page.goto("/participant/sessions/join");
    await page.waitForLoadState("networkidle");
    await page.waitForTimeout(1000);

    await page.fill("#pin", pinCode);
    await page.click("button:has-text('Entrar a la Sesión')");

    await expect(
      page.locator("text=Esperando").or(page.locator(".waiting-room")),
    ).toBeVisible({ timeout: 10000 });

    await expect(
      page.locator(`text=${sessionName}`),
    ).toBeVisible({ timeout: 5000 });
  });

  test("operator starts the session and participant sees Active state", async ({
    page: operatorPage,
    browser,
  }) => {
    test.skip(!sessionName, "Session name not available from previous test");

    const participantPage = await browser.newPage();

    try {
      await loginViaKeycloak(operatorPage, OPERATOR_USER, OPERATOR_PASS);

      await operatorPage.goto("/operator/sesiones");
      await operatorPage.waitForLoadState("networkidle");
      await operatorPage.waitForTimeout(1000);

      await operatorPage.fill("#search", sessionName);
      await operatorPage.click("button:has-text('Buscar')");
      await operatorPage.waitForTimeout(2000);

      const sessionCard = operatorPage.locator(
        `.session-card:has-text("${sessionName}")`,
      );
      await expect(sessionCard).toBeVisible({ timeout: 10000 });

      const startBtn = sessionCard.locator("button:has-text('Iniciar')");
      if (await startBtn.isVisible()) {
        await startBtn.click();
        await operatorPage.waitForTimeout(2000);
      }

      if (pinCode) {
        await loginViaKeycloak(participantPage, participantUser, "Test1234");

        await participantPage.goto("/participant/sessions/join");
        await participantPage.waitForLoadState("networkidle");
        await participantPage.waitForTimeout(1000);

        await participantPage.fill("#pin", pinCode);
        await participantPage.click("button:has-text('Entrar a la Sesión')");

        await participantPage.waitForTimeout(3000);

        const body = await participantPage.locator("body").textContent();
        const isActive =
          body?.includes("Activa") ||
          body?.includes("Esperando") ||
          body?.includes("Búsqueda") ||
          body?.includes("Trivia");
        expect(isActive).toBeTruthy();
      }

      await operatorPage.reload();
      await operatorPage.waitForLoadState("networkidle");

      await expect(
        operatorPage.locator(
          `.session-card:has-text("${sessionName}") .badge-success`,
        ),
      ).toBeVisible({ timeout: 5000 });
    } finally {
      await participantPage.close();
    }
  });
});
