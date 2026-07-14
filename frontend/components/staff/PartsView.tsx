import { Button } from "@/components/shared/Button";
import { formatCurrency } from "@/lib/mock/data";
import styles from "./PartsView.module.css";

interface Part { id: number; name: string; sku: string; unitPrice: number; stockQuantity: number }

interface PartsViewProps {
  parts: Part[];
  showForm: boolean;
  setShowForm: (v: boolean) => void;
  name: string;
  setName: (v: string) => void;
  sku: string;
  setSku: (v: string) => void;
  unitPrice: string;
  setUnitPrice: (v: string) => void;
  stockQuantity: string;
  setStockQuantity: (v: string) => void;
  addPart: (e: React.FormEvent) => void;
  lowStockThreshold: number;
}

export function PartsView({ parts, showForm, setShowForm, name, setName, sku, setSku, unitPrice, setUnitPrice, stockQuantity, setStockQuantity, addPart, lowStockThreshold }: PartsViewProps) {
  return (
    <div>
      <div className={styles.header}>
        <h1 className={styles.title}>Phụ tùng</h1>
        <Button onClick={() => setShowForm(!showForm)}>{showForm ? "Đóng" : "+ Thêm phụ tùng mới"}</Button>
      </div>

      {showForm && (
        <form className={styles.form} onSubmit={addPart}>
          <input className={styles.input} placeholder="Tên phụ tùng" value={name} onChange={(e) => setName(e.target.value)} />
          <input className={styles.input} placeholder="SKU" value={sku} onChange={(e) => setSku(e.target.value)} />
          <input className={styles.input} placeholder="Đơn giá" value={unitPrice} onChange={(e) => setUnitPrice(e.target.value)} />
          <input className={styles.input} placeholder="Tồn kho ban đầu" value={stockQuantity} onChange={(e) => setStockQuantity(e.target.value)} />
          <Button type="submit">Lưu</Button>
        </form>
      )}

      <table className={styles.table}>
        <thead>
          <tr>
            <th>Tên phụ tùng</th>
            <th>SKU</th>
            <th className={styles.right}>Đơn giá</th>
            <th className={styles.right}>Tồn kho</th>
          </tr>
        </thead>
        <tbody>
          {parts.map((p) => (
            <tr key={p.id}>
              <td>{p.name}</td>
              <td className={styles.mono}>{p.sku}</td>
              <td className={`${styles.mono} ${styles.right}`}>{formatCurrency(p.unitPrice)}</td>
              <td className={`${styles.mono} ${styles.right} ${p.stockQuantity <= lowStockThreshold ? styles.low : styles.ok}`}>{p.stockQuantity}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <p className={styles.note}>Tồn kho chỉ tự động đổi khi hạng mục được đánh dấu "đã dùng" — không sửa tay được ở đây.</p>
    </div>
  );
}
