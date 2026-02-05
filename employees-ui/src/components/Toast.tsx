import { useEffect } from "react";

type Props = {
  message: string;
  type: "success" | "error";
  onClose: () => void;
  duration?: number;
};

export default function Toast({
  message,
  type,
  onClose,
  duration = 3000,
}: Props) {
  useEffect(() => {
    const t = setTimeout(onClose, duration);
    return () => clearTimeout(t);
  }, [onClose, duration]);

  return (
    <div style={{ ...toast, ...(type === "success" ? success : error) }}>
      {message}
    </div>
  );
}

const toast: React.CSSProperties = {
  position: "fixed",
  top: 20,
  right: 20,
  padding: "12px 16px",
  borderRadius: 8,
  color: "white",
  fontWeight: 500,
  zIndex: 9999,
  boxShadow: "0 4px 12px rgba(0,0,0,0.15)",
};

const success: React.CSSProperties = {
  background: "#16a34a",
};

const error: React.CSSProperties = {
  background: "#dc2626",
};
