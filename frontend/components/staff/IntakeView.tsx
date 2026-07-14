import { Button } from "@/components/shared/Button";
import styles from "./IntakeView.module.css";

interface Vehicle { id: number; licensePlate: string; label: string }
interface FoundCustomer { fullName: string; phone: string; email: string; vehicles: Vehicle[] }

interface IntakeViewProps {
  phone: string;
  setPhone: (v: string) => void;
  foundCustomer: FoundCustomer | null;
  searched: boolean;
  searchByPhone: () => void;
  selectedVehicleId: number | null;
  setSelectedVehicleId: (id: number) => void;
  description: string;
  setDescription: (v: string) => void;
  hasOpenWorkOrderWarning: boolean;
  submit: (e: React.FormEvent) => void;
  submitted: boolean;
}

export function IntakeView({ phone, setPhone, foundCustomer, searched, searchByPhone, selectedVehicleId, setSelectedVehicleId, description, setDescription, hasOpenWorkOrderWarning, submit, submitted }: IntakeViewProps) {
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
            <input className={styles.input} placeholder="Số điện thoại (thử 0912 345 678)" value={phone} onChange={(e) => setPhone(e.target.value)} />
            <Button type="button" variant="secondary" onSteel onClick={searchByPhone}>Tìm</Button>
          </div>

          {searched && foundCustomer && (
            <div className={styles.customerCard} style={{ marginTop: 12 }}>
              <div className={styles.customerName}>{foundCustomer.fullName}</div>
              <div className={styles.customerMeta}>{foundCustomer.phone} · {foundCustomer.email}</div>
              <div className={styles.chips}>
                {foundCustomer.vehicles.map((v) => (
                  <button
                    type="button"
                    key={v.id}
                    className={`${styles.chip} ${selectedVehicleId === v.id ? styles.chipActive : ""}`}
                    onClick={() => setSelectedVehicleId(v.id)}
                  >
                    {v.licensePlate} · {v.label}
                  </button>
                ))}
              </div>
            </div>
          )}

          {searched && !foundCustomer && (
            <div className={styles.notFound} style={{ marginTop: 12 }}>
              Không tìm thấy khách hàng với số điện thoại này.
              <div>
                <button type="button" className={styles.newCustomerBtn}>+ Tạo khách hàng mới</button>
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
    </div>
  );
}
