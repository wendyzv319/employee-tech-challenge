import type { Employee } from "../types/employee";
import { emitLogout } from "./authEvents";

//const BASE_URL = (import.meta.env.VITE_API_URL ?? "http://localhost:8080").replace(/\/$/, "");
const BASE_URL = "";

const TOKEN_KEY = "auth_token";

let token: string | null = localStorage.getItem(TOKEN_KEY);

export function setAuthToken(newToken: string | null) {
    token = newToken;
    if (newToken) localStorage.setItem(TOKEN_KEY, newToken);
    else localStorage.removeItem(TOKEN_KEY);
}

export function getAuthToken() {
    return token;
}

export type CreateEmployeeDto = Omit<Employee, "id"> & { password: string };
export type UpdateEmployeeDto = Partial<Omit<Employee, "id">>;


type EmployeeKey = number; // documentNumber

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
    const headers: Record<string, string> = {
        ...(options.headers as Record<string, string> | undefined),
    };

    // Only set JSON content-type when body exists
    const hasBody = options.body !== undefined && options.body !== null;
    if (hasBody) headers["Content-Type"] = "application/json";

    if (token) headers.Authorization = `Bearer ${token}`;

    const res = await fetch(`${BASE_URL}${path}`, { ...options, headers });

    if (res.status === 401) {
        setAuthToken(null);
        emitLogout();
        throw new Error("Session expired. Please sign in again.");
    }

    if (!res.ok) {
        const text = await res.text().catch(() => "");
        throw new Error(text || `Request failed: ${res.status}`);
    }

    // 204 No Content
    if (res.status === 204) return undefined as T;

    const contentType = res.headers.get("content-type") || "";
    if (contentType.includes("application/json")) return (await res.json()) as T;
    return (await res.text()) as unknown as T;
}

export const api = {
    login: (documentNumber: number, password: string) =>
        request<{
            token: string;
            documentNumber: number;
            email: string;
            role: number;
        }>("/api/Auth/login", {
            method: "POST",
            body: JSON.stringify({ documentNumber, password }),
        }),

    getEmployees: () => request<Employee[]>("/api/Employees"),

    createEmployee: (dto: CreateEmployeeDto) =>
        request<Employee>("/api/Employees", {
            method: "POST",
            body: JSON.stringify(dto),
        }),

    updateEmployee: (dto: UpdateEmployeeDto) =>
        request<Employee>(`/api/Employees`, {
            method: "PUT",
            body: JSON.stringify(dto),
        }),


    deleteEmployee: (documentNumber: EmployeeKey) =>
        request<void>(`/api/Employees/${documentNumber}`, {
            method: "DELETE",
        }),
};
