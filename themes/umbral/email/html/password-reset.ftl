<html>
<body style="margin:0;padding:0;background-color:#1a1a2e;font-family:Arial,Helvetica,sans-serif;">
<table width="100%" cellpadding="0" cellspacing="0" style="background-color:#1a1a2e;min-height:100vh;">
<tr><td align="center" style="padding:40px 16px;">
  <table width="100%" style="max-width:520px;">
    <tr>
      <td style="text-align:center;padding-bottom:24px;">
        <span style="font-size:28px;font-weight:800;color:#e94560;">UMBRAL</span>
        <span style="display:block;font-size:13px;color:#666;margin-top:2px;">Experiencias de investigaci&oacute;n inmersiva</span>
      </td>
    </tr>
    <tr>
      <td style="background-color:#16213e;border-radius:12px;padding:36px 32px;text-align:center;">
        <h1 style="color:#ffffff;font-size:22px;margin:0 0 16px 0;">Establec&eacute; tu contrase&ntilde;a</h1>
        <p style="color:#b0b0b0;font-size:15px;line-height:1.6;margin:0 0 28px 0;">
          Hola <strong style="color:#ffffff;">${user.firstName!}!</strong><br>
          Un administrador te ha creado una cuenta en <strong style="color:#e94560;">UMBRAL</strong>.
          Hac&eacute; clic en el bot&oacute;n para establecer tu contrase&ntilde;a y acceder a la plataforma.
        </p>
        <a href="${link}" target="_blank" rel="noopener" style="display:inline-block;background-color:#e94560;color:#ffffff;text-decoration:none;font-size:16px;font-weight:700;padding:14px 40px;border-radius:8px;">
          Establecer contrase&ntilde;a
        </a>
        <p style="color:#666;font-size:13px;margin:24px 0 0 0;line-height:1.5;">
          Este enlace expira en <strong style="color:#e94560;">${linkExpiration!} horas</strong>.<br>
          Si no esper&aacute;as este correo, ignor&aacute; este mensaje.
        </p>
      </td>
    </tr>
    <tr>
      <td style="text-align:center;padding-top:20px;">
        <p style="color:#444;font-size:12px;margin:0;">
          UMBRAL &mdash; UCAB &bull; Desarrollo de Software
        </p>
      </td>
    </tr>
  </table>
</td></tr>
</table>
</body>
</html>
