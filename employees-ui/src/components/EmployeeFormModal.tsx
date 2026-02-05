import { useEffect, useMemo, useState } from "react";
import type { Employee } from "../types/employee";
import { ROLE_OPTIONS } from "../constants/roles";
import { GENDER_OPTIONS } from "../constants/gender";


type FormValue = {
    documentNumber: string;
    firstName: string;
    lastName: string;
    email: string;
    birthDate: string; // yyyy-mm-dd
    gender: string; // number as string
    role: string;   // number as string
    managerDocumentNumber: string; // "" = none
    phones: string; // comma-separated
    password: string; // only for create
};

type Props = {
    open: boolean;
    mode: "create" | "edit";
    employees: Employee[];
    initialEmployee?: Employee | null;
    onClose: () => void;
    onSubmit: (payload: any) => Promise<void>; // we build dto in parent
};

export default function EmployeeFormModal({
    open,
    mode,
    employees,
    initialEmployee,
    onClose,
    onSubmit,
}: Props) {
    const [v, setV] = useState<FormValue>({
        documentNumber: "",
        firstName: "",
        lastName: "",
        email: "",
        birthDate: "",
        gender: "1",
        role: "1",
        managerDocumentNumber: "",
        phones: "",
        password: "",
    });

    const [formError, setFormError] = useState<string | null>(null);

    const title = mode === "create" ? "Add new employee" : "Edit employee";

    useEffect(() => {
        if (!open) return;

        setFormError(null);

        if (mode === "edit" && initialEmployee) {
            setV({
                documentNumber: String(initialEmployee.documentNumber),
                firstName: initialEmployee.firstName ?? "",
                lastName: initialEmployee.lastName ?? "",
                email: initialEmployee.email ?? "",
                birthDate: (initialEmployee.birthDate ?? "").slice(0, 10),
                gender: String(initialEmployee.gender ?? 1),
                role: String(initialEmployee.role ?? 1),
                managerDocumentNumber: initialEmployee.managerDocumentNumber
                    ? String(initialEmployee.managerDocumentNumber)
                    : "",
                phones: Array.isArray(initialEmployee.phones)
                    ? initialEmployee.phones.join(",")
                    : "",
                password: "",
            });
        } else {
            setV({
                documentNumber: "",
                firstName: "",
                lastName: "",
                email: "",
                birthDate: "",
                gender: "1",
                role: "1",
                managerDocumentNumber: "",
                phones: "",
                password: "",
            });
        }
    }, [open, mode, initialEmployee]);

    const managerOptions = useMemo(() => {
        const list = employees.slice().sort((a, b) => a.documentNumber - b.documentNumber);
        return list;
    }, [employees]);

    if (!open) return null;

    async function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        setFormError(null);

        const phonesArr = v.phones
            .split(",")
            .map((x) => x.trim())
            .filter(Boolean)
            .map((x) => Number(x))
            .filter((n) => Number.isFinite(n) && n > 0);

        if (phonesArr.length < 2) {
            setFormError("Please enter at least 2 phone numbers (comma-separated).");
            return;
        }

        if (!v.birthDate) {
            setFormError("Birth date is required.");
            return;
        }

        const payload: any = {
            documentNumber: Number(v.documentNumber),
            firstName: v.firstName.trim(),
            lastName: v.lastName.trim(),
            email: v.email.trim(),
            birthDate: `${v.birthDate}T00:00:00`,
            gender: Number(v.gender),
            role: Number(v.role),
            managerDocumentNumber: v.managerDocumentNumber ? Number(v.managerDocumentNumber) : null,
            phones: phonesArr,
        };

        if (mode === "create") payload.password = v.password;

        await onSubmit(payload);
    }


    return (
        <div style={backdrop} onMouseDown={onClose}>
            <div style={modal} onMouseDown={(e) => e.stopPropagation()}>
                <div style={{ display: "flex", justifyContent: "space-between", gap: 12 }}>
                    <h2 style={{ margin: 0 }}>{title}</h2>
                    <button onClick={onClose} style={xBtn}>✕</button>
                </div>

                <form onSubmit={handleSubmit} style={{ display: "grid", gap: 12, marginTop: 12 }}>
                    <div style={row2}>
                        <label>
                            Document Number
                            <input
                                value={v.documentNumber}
                                onChange={(e) => setV({ ...v, documentNumber: e.target.value })}
                                type="number"
                                required
                                disabled={mode === "edit"} // normally you can't change it
                                style={input}
                            />
                        </label>

                        <label>
                            Role
                            <select
                                value={v.role}
                                onChange={(e) => setV({ ...v, role: e.target.value })}
                                style={input}
                            >
                                {ROLE_OPTIONS.map((r) => (
                                    <option key={r.value} value={r.value}>
                                        {r.label}
                                    </option>
                                ))}
                            </select>
                        </label>
                    </div>

                    <div style={row2}>
                        <label>
                            First name
                            <input
                                value={v.firstName}
                                onChange={(e) => setV({ ...v, firstName: e.target.value })}
                                required
                                style={input}
                            />
                        </label>

                        <label>
                            Last name
                            <input
                                value={v.lastName}
                                onChange={(e) => setV({ ...v, lastName: e.target.value })}
                                required
                                style={input}
                            />
                        </label>
                    </div>

                    <div style={row2}>
                        <label>
                            Email
                            <input
                                value={v.email}
                                onChange={(e) => setV({ ...v, email: e.target.value })}
                                type="email"
                                required
                                style={input}
                            />
                        </label>

                        <label>
                            Birth date
                            <input
                                value={v.birthDate}
                                onChange={(e) => setV({ ...v, birthDate: e.target.value })}
                                type="date"
                                required
                                style={input}
                            />
                        </label>
                    </div>

                    <div style={row2}>
                        <label>
                            Gender
                            <select
                                value={v.gender}
                                onChange={(e) => setV({ ...v, gender: e.target.value })}
                                style={input}
                            >
                                {GENDER_OPTIONS.map((g) => (
                                    <option key={g.value} value={g.value}>
                                        {g.label}
                                    </option>
                                ))}
                            </select>
                        </label>


                        <label>
                            Manager name (Manager can be employee)
                            <select
                                value={v.managerDocumentNumber}
                                onChange={(e) => setV({ ...v, managerDocumentNumber: e.target.value })}
                                style={input}
                            >
                                <option value="">No manager</option>
                                {managerOptions.map((m) => (
                                    <option key={m.id} value={m.documentNumber}>
                                        {m.firstName} {m.lastName} ({m.documentNumber})
                                    </option>
                                ))}
                            </select>
                        </label>
                    </div>

                    <label>
                        Phones (comma-separated)
                        <input
                            value={v.phones}
                            onChange={(e) => setV({ ...v, phones: e.target.value })}
                            placeholder="11999999999,11888888888"
                            style={input}
                        />
                    </label>

                    {mode === "create" && (
                        <label>
                            Password
                            <input
                                value={v.password}
                                onChange={(e) => setV({ ...v, password: e.target.value })}
                                type="password"
                                required
                                style={input}
                            />
                        </label>
                    )}

                    {formError && (
                        <div
                            style={{
                                whiteSpace: "pre-line",
                                background: "#FEF2F2",
                                border: "1px solid #FCA5A5",
                                color: "#991B1B",
                                padding: 10,
                                borderRadius: 8,
                            }}
                        >
                            {formError}
                        </div>
                    )}


                    <div style={{ display: "flex", gap: 10, justifyContent: "flex-end", marginTop: 6 }}>
                        <button type="button" onClick={onClose} style={btn}>
                            Cancel
                        </button>
                        <button type="submit" style={btnPrimary}>
                            Save
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}

const backdrop: React.CSSProperties = {
    position: "fixed",
    inset: 0,
    background: "rgba(0,0,0,0.45)",
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    padding: 16,
    zIndex: 999,
};

const modal: React.CSSProperties = {
    width: "min(800px, 96vw)",
    background: "white",
    borderRadius: 12,
    padding: 16,
};

const row2: React.CSSProperties = {
    display: "grid",
    gridTemplateColumns: "1fr 1fr",
    gap: 12,
};

const input: React.CSSProperties = {
    width: "100%",
    padding: 10,
    marginTop: 6,
};

const btn: React.CSSProperties = {
    padding: "10px 12px",
    cursor: "pointer",
};

const btnPrimary: React.CSSProperties = {
    ...btn,
    border: "1px solid #111",
    background: "#111",
    color: "white",
};

const xBtn: React.CSSProperties = {
    border: "none",
    background: "transparent",
    cursor: "pointer",
    fontSize: 18,
    lineHeight: 1,
};
