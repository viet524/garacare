import Link from "next/link";
import { TopNav } from "@/components/customer/TopNav";
import { Button } from "@/components/shared/Button";
import { Modal } from "@/components/shared/Modal";
import type { VehicleResponse } from "@/types/vehicle";
import styles from "./VehicleProfileView.module.css";

interface VehicleProfileViewProps {
  vehicles: VehicleResponse[];
  loading: boolean;
  error: string | null;
  showAddModal: boolean;
  openAddModal: () => void;
  closeAddModal: () => void;
  licensePlate: string;
  setLicensePlate: (v: string) => void;
  brand: string;
  setBrand: (v: string) => void;
  model: string;
  setModel: (v: string) => void;
  saving: boolean;
  formError: string | null;
  fieldErrors: Record<string, string>;
  createNewVehicle: () => void;
}

export function VehicleProfileView({
  vehicles,
  loading,
  error,
  showAddModal,
  openAddModal,
  closeAddModal,
  licensePlate,
  setLicensePlate,
  brand,
  setBrand,
  model,
  setModel,
  saving,
  formError,
  fieldErrors,
  createNewVehicle,
}: VehicleProfileViewProps) {
  return (
    <div className={styles.page}>
      <TopNav />
      <div className={styles.content}>
        <div className={styles.header}>
          <h1 className={styles.title}>Xe của tôi</h1>
          <button type="button" className={styles.addBtn} onClick={openAddModal}>+ Thêm xe mới</button>
        </div>

        {loading && <p>Đang tải danh sách xe…</p>}
        {error && <p>{error}</p>}
        {!loading && !error && vehicles.length === 0 && <p>Bạn chưa có xe nào được đăng ký.</p>}

        <div className={styles.rows}>
          {vehicles.map((v) => (
            <div key={v.id} className={styles.row}>
              <div>
                <div className={styles.plate}>{v.licensePlate}</div>
                <div className={styles.meta}>{v.brand} {v.model} · {v.year}</div>
              </div>
              <Link href={`/customer/vehicles/${v.id}/history`}>Xem lịch sử sửa chữa →</Link>
            </div>
          ))}
        </div>
      </div>

      {showAddModal && (
        <Modal title="Thêm xe mới" onClose={closeAddModal} theme="light">
          <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
            <div>
              <input className={`${styles.input} ${fieldErrors.licensePlate ? styles.inputError : ""}`} placeholder="Biển số" value={licensePlate} onChange={(e) => setLicensePlate(e.target.value)} />
              {fieldErrors.licensePlate && <div className={styles.fieldError}>{fieldErrors.licensePlate}</div>}
            </div>
            <input className={styles.input} placeholder="Hãng xe" value={brand} onChange={(e) => setBrand(e.target.value)} />
            <input className={styles.input} placeholder="Dòng xe" value={model} onChange={(e) => setModel(e.target.value)} />
            {formError && <div className={styles.formError}>{formError}</div>}
            <Button type="button" fullWidth onClick={createNewVehicle} disabled={saving || !licensePlate.trim()}>
              {saving ? "Đang thêm…" : "Thêm xe"}
            </Button>
          </div>
        </Modal>
      )}
    </div>
  );
}
