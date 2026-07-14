import Link from "next/link";
import { TopNav } from "@/components/customer/TopNav";
import styles from "./VehicleProfileView.module.css";

interface Vehicle { id: number; licensePlate: string; brand: string; model: string; year: number }

interface VehicleProfileViewProps {
  vehicles: Vehicle[];
}

export function VehicleProfileView({ vehicles }: VehicleProfileViewProps) {
  return (
    <div className={styles.page}>
      <TopNav />
      <div className={styles.content}>
        <div className={styles.header}>
          <h1 className={styles.title}>Xe của tôi</h1>
          <button className={styles.addBtn}>+ Thêm xe mới</button>
        </div>
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
    </div>
  );
}
