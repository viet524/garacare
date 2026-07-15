import { Button } from "@/components/shared/Button";
import { Modal } from "@/components/shared/Modal";
import type { CustomerResponse } from "@/types/customer";
import styles from "./CustomerManagementView.module.css";

interface CustomerManagementViewProps {
  customers: CustomerResponse[];
  loading: boolean;
  error: string | null;
  showAddModal: boolean;
  openAddModal: () => void;
  closeAddModal: () => void;
  fullName: string;
  setFullName: (v: string) => void;
  phone: string;
  setPhone: (v: string) => void;
  email: string;
  setEmail: (v: string) => void;
  address: string;
  setAddress: (v: string) => void;
  saving: boolean;
  formError: string | null;
  fieldErrors: Record<string, string>;
  createNewCustomer: () => void;
}

export function CustomerManagementView({
  customers,
  loading,
  error,
  showAddModal,
  openAddModal,
  closeAddModal,
  fullName,
  setFullName,
  phone,
  setPhone,
  email,
  setEmail,
  address,
  setAddress,
  saving,
  formError,
  fieldErrors,
  createNewCustomer,
}: CustomerManagementViewProps) {
  return (
    <div>
      <div className={styles.header}>
        <h1 className={styles.title}>Quản lý khách hàng</h1>
        <Button type="button" onClick={openAddModal}>+ Tạo khách hàng mới</Button>
      </div>

      {loading && <p>Đang tải danh sách khách hàng…</p>}
      {error && <p>{error}</p>}

      {!loading && !error && (
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Họ tên</th>
              <th>Số điện thoại</th>
              <th>Email</th>
              <th>Địa chỉ</th>
              <th>Loại</th>
            </tr>
          </thead>
          <tbody>
            {customers.map((c) => (
              <tr key={c.id}>
                <td>{c.fullName}</td>
                <td className={styles.mono}>{c.phone ?? "—"}</td>
                <td className={styles.mono}>{c.email ?? "—"}</td>
                <td>{c.address ?? "—"}</td>
                <td>
                  <span className={`${styles.badge} ${c.userId ? styles.badgeAccount : styles.badgeWalkIn}`}>
                    {c.userId ? "Có tài khoản" : "Vãng lai"}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {showAddModal && (
        <Modal title="Tạo khách hàng mới" onClose={closeAddModal}>
          <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
            <div>
              <input className={`${styles.input} ${fieldErrors.fullName ? styles.inputError : ""}`} placeholder="Họ tên khách hàng" value={fullName} onChange={(e) => setFullName(e.target.value)} />
              {fieldErrors.fullName && <div className={styles.fieldError}>{fieldErrors.fullName}</div>}
            </div>
            <div>
              <input className={`${styles.input} ${fieldErrors.phone ? styles.inputError : ""}`} placeholder="Số điện thoại" value={phone} onChange={(e) => setPhone(e.target.value)} />
              {fieldErrors.phone && <div className={styles.fieldError}>{fieldErrors.phone}</div>}
            </div>
            <div>
              <input className={`${styles.input} ${fieldErrors.email ? styles.inputError : ""}`} placeholder="Email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
              {fieldErrors.email && <div className={styles.fieldError}>{fieldErrors.email}</div>}
            </div>
            <input className={styles.input} placeholder="Địa chỉ (tuỳ chọn)" value={address} onChange={(e) => setAddress(e.target.value)} />
            {formError && <div className={styles.formError}>{formError}</div>}
            <Button type="button" fullWidth onClick={createNewCustomer} disabled={saving || !fullName.trim() || !phone.trim() || !email.trim()}>
              {saving ? "Đang tạo…" : "+ Tạo khách hàng mới"}
            </Button>
          </div>
        </Modal>
      )}
    </div>
  );
}
