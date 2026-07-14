import { Button } from "@/components/shared/Button";
import { StatusBadge } from "@/components/shared/StatusBadge";
import type { AppointmentView } from "@/types/domain";
import styles from "./CheckInView.module.css";

interface CheckInViewProps {
  phone: string;
  setPhone: (v: string) => void;
  results: AppointmentView[] | null;
  searchByPhone: () => void;
  checkedInId: number | null;
  checkIn: (id: number) => void;
}

export function CheckInView({ phone, setPhone, results, searchByPhone, checkedInId, checkIn }: CheckInViewProps) {
  return (
    <div>
      <h1 className={styles.title}>Check-in lịch hẹn</h1>
      <div className={styles.searchRow}>
        <input className={styles.input} placeholder="Số điện thoại khách hàng" value={phone} onChange={(e) => setPhone(e.target.value)} />
        <Button variant="secondary" onSteel onClick={searchByPhone}>Tìm</Button>
      </div>

      {results && results.length > 0 && (
        <div className={styles.rows}>
          {results.map((ap) => (
            <div key={ap.id} className={styles.row}>
              <div className={styles.rowLeft}>
                <div>
                  <div className={styles.time}>{ap.scheduledTimeSlot}</div>
                  <div className={styles.plate}>{ap.licensePlate} · {ap.vehicleLabel}</div>
                </div>
                <StatusBadge status={ap.status} kind="appointment" onSteel />
              </div>
              <Button onClick={() => checkIn(ap.id)} disabled={checkedInId === ap.id}>
                {checkedInId === ap.id ? "Đã check-in" : "Check-in"}
              </Button>
            </div>
          ))}
        </div>
      )}

      {results && results.length === 0 && (
        <p className={styles.empty}>
          Không tìm thấy lịch hẹn nào.
          <br />
          <button className={styles.linkBtn}>Chuyển sang Tiếp nhận xe →</button>
        </p>
      )}

      {checkedInId && <div className={styles.done}>Đã tạo Work Order mới từ lịch hẹn, kế thừa ưu đãi đặt trước.</div>}
    </div>
  );
}
