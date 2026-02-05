
export const GENDER_LABELS: Record<number, string> = {
  1: "Male",
  2: "Female",
};

export const GENDER_OPTIONS = Object.entries(GENDER_LABELS).map(
  ([value, label]) => ({
    value: Number(value),
    label,
  })
);
