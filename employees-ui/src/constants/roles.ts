export const ROLES = {
  EMPLOYEE: 1,
  MANAGER: 2,
  ADMIN: 3,
} as const;

export const ROLE_LABELS: Record<number, string> = {
  1: "Employee",
  2: "Manager",
  3: "Admin",
};

export const ROLE_OPTIONS = Object.entries(ROLE_LABELS).map(([value, label]) => ({
  value: Number(value),
  label,
}));