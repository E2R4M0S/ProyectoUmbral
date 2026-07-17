// Keycloak returns every realm role a user has, including built-ins like
// "default-roles-umbral", "offline_access" and "uma_authorization" — none of
// which are meaningful to an admin browsing the user list. Only the three
// application roles are ever shown in the UI.
export const KNOWN_ROLES = ["admin", "operator", "participant"] as const;
export type KnownRole = (typeof KNOWN_ROLES)[number];

export function isKnownRole(role: string): role is KnownRole {
  return (KNOWN_ROLES as readonly string[]).includes(role);
}

export function filterKnownRoles(roles: string[]): KnownRole[] {
  return roles.filter(isKnownRole);
}

export function roleClass(r: string): string {
  if (r === "admin")       return "badge badge-admin";
  if (r === "operator")    return "badge badge-operator";
  if (r === "participant") return "badge badge-participant";
  return "badge badge-muted";
}

export function roleLabel(r: string): string {
  if (r === "admin")       return "Admin";
  if (r === "operator")    return "Operador";
  if (r === "participant") return "Participante";
  return r;
}
