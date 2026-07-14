import { TopNav } from "@/components/customer/TopNav";
import { Button } from "@/components/shared/Button";
import styles from "./BookAppointmentView.module.css";

interface Vehicle { id: number; licensePlate: string; label: string }

interface BookAppointmentViewProps {
  vehicles: Vehicle[];
  vehicleId: number | null;
  setVehicleId: (id: number) => void;
  date: string;
  setDate: (v: string) => void;
  timeSlots: string[];
  timeSlot: string | null;
  selectSlot: (slot: string) => void;
  bookedCountFor: (slot: string) => number;
  slotCapacity: number;
  discountPercent: number;
  confirmed: boolean;
  confirm: () => void;
  slotFullError: string | null;
}

export function BookAppointmentView({ vehicles, vehicleId, setVehicleId, date, setDate, timeSlots, timeSlot, selectSlot, bookedCountFor, slotCapacity, discountPercent, confirmed, confirm, slotFullError }: BookAppointmentViewProps) {
  return (
    <div className={styles.page}>
      <TopNav />
      <div className={styles.content}>
        <h1 className={styles.title}>Đặt lịch hẹn</h1>

        {confirmed ? (
          <div className={styles.success}>Đã đặt lịch thành công cho ngày {date}, khung giờ {timeSlot}. Chúng tôi đã gửi xác nhận qua email.</div>
        ) : (
          <div className={styles.layout}>
            <div className={styles.calendarCard}>
              <div className={styles.dateRow}>
                <input type="date" className={styles.dateInput} value={date} onChange={(e) => setDate(e.target.value)} />
              </div>
              <div className={styles.slotGrid}>
                {timeSlots.map((slot) => {
                  const count = bookedCountFor(slot);
                  const full = count >= slotCapacity;
                  const selected = timeSlot === slot;
                  return (
                    <button
                      key={slot}
                      type="button"
                      className={`${styles.slot} ${full ? styles.slotFull : ""} ${selected ? styles.slotSelected : ""}`}
                      disabled={full}
                      onClick={() => selectSlot(slot)}
                    >
                      {slot}
                      <span className={styles.slotCount}>{full ? "Đã đầy" : `Còn ${slotCapacity - count} chỗ`}</span>
                    </button>
                  );
                })}
              </div>
            </div>

            <div className={styles.panel}>
              <select className={styles.select} value={vehicleId ?? ""} onChange={(e) => setVehicleId(Number(e.target.value))}>
                <option value="" disabled>Chọn xe</option>
                {vehicles.map((v) => (
                  <option key={v.id} value={v.id}>{v.licensePlate} · {v.label}</option>
                ))}
              </select>
              <div className={styles.summaryLine}>
                Khung giờ: <span className={styles.summaryMono}>{timeSlot ?? "chưa chọn"}</span>
              </div>
              <span className={styles.discountTag}>-{discountPercent}% đặt trước</span>
              {slotFullError && <p className={styles.errorText}>{slotFullError}</p>}
              <Button fullWidth disabled={!vehicleId || !timeSlot} onClick={confirm}>Xác nhận đặt lịch</Button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
