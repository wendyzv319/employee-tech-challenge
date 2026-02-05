export function extractApiErrorMessage(err: unknown): string {
  if (err instanceof Error) {
    const msg = err.message?.trim();
    if (!msg) return "Something went wrong.";

    // Try parse ASP.NET ValidationProblemDetails JSON (it comes as text)
    try {
      const parsed = JSON.parse(msg);

      // { title, errors: { Field: [msg] } }
      if (parsed?.errors && typeof parsed.errors === "object") {
        const messages: string[] = [];
        for (const [field, arr] of Object.entries(parsed.errors)) {
          if (Array.isArray(arr)) {
            for (const m of arr) messages.push(`${field}: ${m}`);
          }
        }
        if (messages.length) return messages.join("\n");
      }

      if (typeof parsed?.title === "string") return parsed.title;
      return msg;
    } catch {
      return msg; // not JSON
    }
  }

  return "Something went wrong.";
}
