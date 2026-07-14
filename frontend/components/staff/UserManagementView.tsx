import { Button } from "@/components/shared/Button";
import styles from "./PartsView.module.css";

type InternalRole = "Staff" | "Technician" | "Admin";
interface InternalUser { id: number; username: string; fullName: string; phone: string; role: InternalRole }

interface UserManagementViewProps {
  users: InternalUser[];
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

export function UserManagementView({ users, showForm, setShowForm, username, setUsername, fullName, setFullName, phone, setPhone, role, setRole, addUser }: UserManagementViewProps) {
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

      <table className={styles.table}>
        <thead>
          <tr>
            <th>Username</th>
            <th>Họ tên</th>
            <th>SĐT</th>
            <th>Vai trò</th>
          </tr>
        </thead>
        <tbody>
          {users.map((u) => (
            <tr key={u.id}>
              <td className={styles.mono}>{u.username}</td>
              <td>{u.fullName}</td>
              <td className={styles.mono}>{u.phone}</td>
              <td>{u.role}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
