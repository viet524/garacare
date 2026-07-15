import { Button } from "@/components/shared/Button";
import { Modal } from "@/components/shared/Modal";
import type { CustomerResponse } from "@/types/customer";
import type { VehicleResponse } from "@/types/vehicle";
import styles from "./IntakeView.module.css";

interface IntakeViewProps {
  phone: string;
  setPhone: (v: string) => void;
  foundCustomer: CustomerResponse | null;
  vehicles: VehicleResponse[];
  searched: boolean;
  loading: boolean;
  error: string | null;
  fieldErrors: Record<string, string>;
  searchByPhone: () => void;
  newCustomerFullName: string;
  setNewCustomerFullName: (v: string) => void;
  newCustomerEmail: string;
  setNewCustomerEmail: (v: string) => void;
  newCustomerAddress: string;
  setNewCustomerAddress: (v: string) => void;
  createNewCustomer: () => void;
  showAddCustomerModal: boolean;
  openAddCustomerModal: () => void;
  closeAddCustomerModal: () => void;
  newVehiclePlate: string;
  setNewVehiclePlate: (v: string) => void;
  newVehicleBrand: string;
  setNewVehicleBrand: (v: string) => void;
  newVehicleModel: string;
  setNewVehicleModel: (v: string) => void;
  addNewVehicle: () => void;
  showAddVehicleModal: boolean;
  openAddVehicleModal: () => void;
  closeAddVehicleModal: () => void;
  selectedVehicleId: number | null;
  setSelectedVehicleId: (id: number) => void;
  description: string;
  setDescription: (v: string) => void;
  hasOpenWorkOrderWarning: boolean;
  submit: (e: React.FormEvent) => void;
  submitted: boolean;
}

export function IntakeView({
  phone,
  setPhone,
  foundCustomer,
  vehicles,
  searched,
  loading,
  error,
  fieldErrors,
  searchByPhone,
  newCustomerFullName,
  setNewCustomerFullName,
  newCustomerEmail,
  setNewCustomerEmail,
  newCustomerAddress,
  setNewCustomerAddress,
  createNewCustomer,
  showAddCustomerModal,
  openAddCustomerModal,
  closeAddCustomerModal,
  newVehiclePlate,
  setNewVehiclePlate,
  newVehicleBrand,
  setNewVehicleBrand,
  newVehicleModel,
  setNewVehicleModel,
  addNewVehicle,
  showAddVehicleModal,
  openAddVehicleModal,
  closeAddVehicleModal,
  selectedVehicleId,
  setSelectedVehicleId,
  description,
  setDescription,
  hasOpenWorkOrderWarning,
  submit,
  submitted,
}: IntakeViewProps) {
  if (submitted) {
    return (
      <div>
        <h1 className={styles.title}>Tiếp nhận xe</h1>
        <div className={styles.card}>
          <div className={styles.success}>Đã tạo Work Order mới ở trạng thái "Đã tiếp nhận". Chuyển Kỹ thuật viên bắt đầu chẩn đoán khi sẵn sàng.</div>
        </div>
      </div>
    );
  }

  return (
    <div>
      <h1 className={styles.title}>Tiếp nhận xe</h1>
      <form className={styles.card} onSubmit={submit}>
        <div>
          <div className={styles.stepLabel}>Bước 1 · Tra cứu khách hàng</div>
          <div className={styles.searchRow} style={{ marginTop: 8 }}>
            <input className={styles.input} placeholder="Số điện thoại" value={phone} onChange={(e) => setPhone(e.target.value)} />
            <Button type="button" variant="secondary" onSteel onClick={searchByPhone} disabled={loading || !phone.trim()}>
              {loading ? "Đang tìm…" : "Tìm"}
            </Button>
          </div>

          {/* Chỉ hiện ở đây khi không có popup nào đang mở — popup mở thì lỗi hiện bên trong
              popup đó, vì overlay (z-index cao) phủ kín trang, hiện ở ngoài sẽ bị che mất. */}
          {error && !showAddCustomerModal && !showAddVehicleModal && (
            <div className={styles.notFound} style={{ marginTop: 12 }}>{error}</div>
          )}

          {searched && foundCustomer && (
            <div className={styles.customerCard} style={{ marginTop: 12 }}>
              <div className={styles.customerName}>{foundCustomer.fullName}</div>
              <div className={styles.customerMeta}>{foundCustomer.phone} · {foundCustomer.email ?? "—"}</div>
              <div className={styles.chips}>
                {vehicles.map((v) => (
                  <button
                    type="button"
                    key={v.id}
                    className={`${styles.chip} ${selectedVehicleId === v.id ? styles.chipActive : ""}`}
                    onClick={() => setSelectedVehicleId(v.id)}
                  >
                    {v.licensePlate} · {v.brand} {v.model}
                  </button>
                ))}
                <button type="button" className={styles.chip} onClick={openAddVehicleModal}>
                  + Thêm xe mới
                </button>
              </div>
            </div>
          )}

          {searched && !foundCustomer && (
            <div className={styles.notFound} style={{ marginTop: 12 }}>
              Không tìm thấy khách hàng với số điện thoại này.
              <div style={{ marginTop: 8 }}>
                <Button type="button" onSteel onClick={openAddCustomerModal}>
                  + Tạo khách hàng mới
                </Button>
              </div>
            </div>
          )}
        </div>

        {hasOpenWorkOrderWarning && (
          <div className={styles.warning}>Xe này đang có 1 lượt sửa chưa hoàn tất — vẫn có thể tiếp tục tiếp nhận.</div>
        )}

        <div>
          <div className={styles.stepLabel}>Bước 2 · Mô tả sự cố</div>
          <textarea
            className={styles.textarea}
            style={{ marginTop: 8, width: "100%" }}
            placeholder="Khách khai báo xe kêu lạ khi phanh…"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
        </div>

        <Button type="submit" fullWidth disabled={!selectedVehicleId || !description}>
          Tiếp nhận xe
        </Button>
      </form>

      {showAddCustomerModal && (
        <Modal title="Tạo khách hàng mới" onClose={closeAddCustomerModal}>
          <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
            <div>
              <input className={`${styles.input} ${fieldErrors.fullName ? styles.inputError : ""}`} placeholder="Họ tên khách hàng" value={newCustomerFullName} onChange={(e) => setNewCustomerFullName(e.target.value)} />
              {fieldErrors.fullName && <div className={styles.fieldError}>{fieldErrors.fullName}</div>}
            </div>
            <div>
              <input className={`${styles.input} ${fieldErrors.email ? styles.inputError : ""}`} placeholder="Email" type="email" value={newCustomerEmail} onChange={(e) => setNewCustomerEmail(e.target.value)} />
              {fieldErrors.email && <div className={styles.fieldError}>{fieldErrors.email}</div>}
            </div>
            <input className={styles.input} placeholder="Địa chỉ (tuỳ chọn)" value={newCustomerAddress} onChange={(e) => setNewCustomerAddress(e.target.value)} />
            {error && <div className={styles.formError}>{error}</div>}
            <Button type="button" fullWidth onClick={createNewCustomer} disabled={loading || !newCustomerFullName.trim() || !newCustomerEmail.trim()}>
              {loading ? "Đang tạo…" : "+ Tạo khách hàng mới"}
            </Button>
          </div>
        </Modal>
      )}

      {showAddVehicleModal && (
        <Modal title="Thêm xe mới" onClose={closeAddVehicleModal}>
          <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
            <div>
              <input className={`${styles.input} ${fieldErrors.licensePlate ? styles.inputError : ""}`} placeholder="Biển số" value={newVehiclePlate} onChange={(e) => setNewVehiclePlate(e.target.value)} />
              {fieldErrors.licensePlate && <div className={styles.fieldError}>{fieldErrors.licensePlate}</div>}
            </div>
            <input className={styles.input} placeholder="Hãng xe" value={newVehicleBrand} onChange={(e) => setNewVehicleBrand(e.target.value)} />
            <input className={styles.input} placeholder="Dòng xe" value={newVehicleModel} onChange={(e) => setNewVehicleModel(e.target.value)} />
            {error && <div className={styles.formError}>{error}</div>}
            <Button type="button" fullWidth onClick={addNewVehicle} disabled={loading || !newVehiclePlate.trim()}>
              {loading ? "Đang thêm…" : "Thêm xe"}
            </Button>
          </div>
        </Modal>
      )}
    </div>
  );
}
