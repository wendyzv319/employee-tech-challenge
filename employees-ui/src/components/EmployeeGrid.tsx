import type { Employee } from "../types/employee";
import { ROLE_LABELS } from "../constants/roles";

type Props = {
    employees: Employee[];
    onEdit: (emp: Employee) => void;
    onDelete: (emp: Employee) => void;
};


export default function EmployeeGrid({ employees, onEdit, onDelete }: Props) {

    return (
        <div style={{ overflowX: "auto" }}>
            <table style={{ width: "100%", borderCollapse: "collapse" }}>
                <thead>
                    <tr>
                        <th style={th}>Document</th>
                        <th style={th}>First name</th>
                        <th style={th}>Last name</th>
                        <th style={th}>Email</th>
                        <th style={th}>Role</th>
                        <th style={th}>Manager</th>
                        <th style={th}>Phones</th>
                        <th style={th}>Actions</th>
                    </tr>
                </thead>
                <tbody>
                    {employees.map((e) => (
                        <tr key={e.id}>
                            <td style={td}>{e.documentNumber}</td>
                            <td style={td}>{e.firstName}</td>
                            <td style={td}>{e.lastName}</td>
                            <td style={td}>{e.email}</td>
                            <td style={td}>{ROLE_LABELS[e.role] ?? `Role ${e.role}`}</td>
                            <td style={td}>{e.managerName ?? "-"}</td>
                            <td style={td}>{e.phones?.length ? e.phones.join(", ") : "-"}</td>
                            <td style={td}>
                                <div style={{ display: "flex", gap: 8 }}>
                                    <button onClick={() => onEdit(e)} style={btn}>
                                        Edit
                                    </button>
                                    <button
                                        onClick={() => onDelete(e)}
                                        style={{ ...btn, borderColor: "crimson", color: "crimson" }}
                                    >
                                        Delete
                                    </button>
                                </div>
                            </td>
                        </tr>
                    ))}
                    {employees.length === 0 && (
                        <tr>
                            <td style={td} colSpan={7}>
                                No employees found.
                            </td>
                        </tr>
                    )}
                </tbody>
            </table>
        </div>
    );
}

const th: React.CSSProperties = {
    textAlign: "left",
    padding: "12px 10px",
    borderBottom: "1px solid #ddd",
    whiteSpace: "nowrap",
};

const td: React.CSSProperties = {
    padding: "12px 10px",
    borderBottom: "1px solid #eee",
    whiteSpace: "nowrap",
};

const btn: React.CSSProperties = {
    padding: "8px 10px",
    border: "1px solid #333",
    borderRadius: 6,
    background: "white",
    cursor: "pointer",
};
