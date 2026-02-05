import { useEffect, useState } from "react";
import EmployeeGrid from "../components/EmployeeGrid";
import { api } from "../services/api";
import type { Employee } from "../types/employee";
import EmployeeFormModal from "../components/EmployeeFormModal";
import Toast from "../components/Toast";
import { extractApiErrorMessage } from "../services/error";


export default function Employees({ onLogout }: { onLogout: () => void }) {
    const [employees, setEmployees] = useState<Employee[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [modalOpen, setModalOpen] = useState(false);
    const [modalMode, setModalMode] = useState<"create" | "edit">("create");
    const [selected, setSelected] = useState<Employee | null>(null);
    const [toast, setToast] = useState<{
        message: string;
        type: "success" | "error";
    } | null>(null);


    async function load() {
        setLoading(true);
        setError(null);
        try {
            const data = await api.getEmployees();
            setEmployees(data);
        } catch (e: any) {
            setError(e?.message ?? "Failed to load employees");
        } finally {
            setLoading(false);
        }
    }

    useEffect(() => {
        load();
    }, []);

    function handleAdd() {
        setSelected(null);
        setModalMode("create");
        setModalOpen(true);
    }

    function handleEdit(emp: Employee) {
        setSelected(emp);
        setModalMode("edit");
        setModalOpen(true);
    }


    async function handleDelete(emp: Employee) {
        const ok = confirm(
            `Delete employee ${emp.firstName} ${emp.lastName} (${emp.documentNumber})?`
        );
        if (!ok) return;

        try {
            await api.deleteEmployee(emp.documentNumber);
            await load();
        } catch (e: any) {
            alert(e?.message ?? "Failed to delete");
        }
    }

    async function handleModalSubmit(payload: any) {
        try {
            if (modalMode === "create") {
                await api.createEmployee(payload);
                setToast({ message: "Employee created successfully", type: "success" });
            } else if (selected) {
                await api.updateEmployee(selected.documentNumber, payload);
                setToast({ message: "Employee updated successfully", type: "success" });
            }

            setModalOpen(false);
            await load();
        } catch (e: any) {
            setToast({ message: extractApiErrorMessage(e), type: "error" });
        }
    }


    return (
        <div style={{ padding: 24 }}>
            <div
                style={{
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "space-between",
                    gap: 12,
                    marginBottom: 16,
                }}
            >
                <h1 style={{ margin: 0 }}>Employees</h1>               
                <div style={{ display: "flex", gap: 10 }}>
                    <button onClick={handleAdd} style={{ padding: "10px 12px", cursor: "pointer" }}>
                        Add new employee
                    </button>
                    <button onClick={onLogout} style={{ padding: "10px 12px", cursor: "pointer" }}>
                        Logout
                    </button>
                </div>
            </div>

            {loading && <p>Loading...</p>}
            {error && <p style={{ color: "crimson" }}>{error}</p>}

            {!loading && !error && (
                <EmployeeGrid
                    employees={employees}
                    onEdit={handleEdit}
                    onDelete={handleDelete}
                />
            )}
            <EmployeeFormModal
                open={modalOpen}
                mode={modalMode}
                employees={employees}
                initialEmployee={selected}
                onClose={() => setModalOpen(false)}
                onSubmit={handleModalSubmit}
            />
            {toast && (
                <Toast
                    message={toast.message}
                    type={toast.type}
                    onClose={() => setToast(null)}
                />
            )}
        </div>

    );

}
