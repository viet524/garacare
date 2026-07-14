import styles from "./StatusStamp.module.css";

interface StatusStampProps {
  outcome: "approved" | "rejected";
}

// Con dấu trạng thái (design.md §5.3) — hiệu ứng khi khách duyệt/từ chối báo giá.
export function StatusStamp({ outcome }: StatusStampProps) {
  const color = outcome === "approved" ? "var(--coolant-teal)" : "var(--brake-red)";
  const text = outcome === "approved" ? "ĐÃ DUYỆT" : "ĐÃ TỪ CHỐI";
  return (
    <div className={styles.stamp} style={{ ["--stamp-color" as string]: color }}>
      {text}
    </div>
  );
}
