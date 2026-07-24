import { Button } from "@/components/shared/Button";
import type { UserResponse } from "@/lib/api/users";
import styles from "./PartsView.module.css";

type InternalRole = "Staff" | "Technician" | "Admin";

const TECHNICIAN_STATUS_LABEL_VI: Record<string, string> = {
  Free: "Rảnh",
  Diagnosing: "Đang chẩn đoán",
  WaitingOnCustomer: "Chờ khách duyệt giá",
  WaitingParts: "Chờ phụ tùng",
  InRepair: "Đang sửa xe",
};

interface UserManagementViewProps {
  users: UserResponse[];
  loading: boolean;
  error: string | null;
  showForm: boolean;
  setShowForm: (v: boolean) => void;
  username: string;
  setUsername: (v: string) => void;
  fullName: string;
  setFullName: (v: string) => void;
  phone: string;
  setPhone: (v: string) => void;
  role: InternalRole;
  setRole: (v: InternalRole) => void;
  addUser: (e: React.FormEvent) => void;
}

export function UserManagementView({ users, loading, error, showForm, setShowForm, username, setUsername, fullName, setFullName, phone, setPhone, role, setRole, addUser }: UserManagementViewProps) {
  return (
    <div>
      <div className={styles.header}>
        <h1 className={styles.title}>Nhân viên</h1>
        <Button onClick={() => setShowForm(!showForm)}>{showForm ? "Đóng" : "+ Thêm nhân viên"}</Button>
      </div>

      {showForm && (
        <form className={styles.form} onSubmit={addUser}>
          <input className={styles.input} placeholder="Username" value={username} onChange={(e) => setUsername(e.target.value)} />
          <input className={styles.input} placeholder="Họ tên" value={fullName} onChange={(e) => setFullName(e.target.value)} />
          <input className={styles.input} placeholder="Số điện thoại" value={phone} onChange={(e) => setPhone(e.target.value)} />
          <select className={styles.input} value={role} onChange={(e) => setRole(e.target.value as InternalRole)}>
            <option value="Staff">Staff</option>
            <option value="Technician">Technician</option>
            <option value="Admin">Admin</option>
          </select>
          <Button type="submit">Lưu</Button>
        </form>
      )}

      {error && <p className={styles.mono}>{error}</p>}
      {!error && (
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Username</th>
              <th>Họ tên</th>
              <th>SĐT</th>
              <th>Vai trò</th>
              <th>Trạng thái</th>
            </tr>
          </thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.id}>
                <td className={styles.mono}>{u.username}</td>
                <td>{u.fullName}</td>
                <td className={styles.mono}>{u.phone}</td>
                <td>{u.role}</td>
                <td>{u.technicianStatus ? TECHNICIAN_STATUS_LABEL_VI[u.technicianStatus] ?? u.technicianStatus : "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {!error && !loading && users.length === 0 && <p className={styles.mono}>Chưa có nhân viên nào.</p>}
    </div>
  );
}
