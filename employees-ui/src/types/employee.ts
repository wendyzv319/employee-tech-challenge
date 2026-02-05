export type Employee = {
  id: number;
  documentNumber: number;
  firstName: string;
  lastName: string;
  email: string;
  birthDate: string; // ISO string
  gender: number;
  role: number;

  managerDocumentNumber: number | null;
  managerName: string | null;

  phones: number[];
};
