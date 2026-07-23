import { Button } from "@/components/shared/Button";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { TicketCard } from "@/components/shared/TicketCard";
import { formatCurrency } from "@/lib/mock/data";
import type { QuotationItemType } from "@/types/domain";
import type { QuotationItemResponse, WorkOrderDetailResponse } from "@/types/workorder";
import { useState } from "react";
import styles from "./QuoteBuilderView.module.css";

interface QuoteBuilderViewProps {
  workOrder: WorkOrderDetailResponse | null;
  loading: boolean;
  error: string | null;
  items: QuotationItemResponse[];
  diagnosisNote: string;
  setDiagnosisNote: (v: string) => void;
  startDiagnosis: () => void;
  newType: QuotationItemType;
  setNewType: (v: QuotationItemType) => void;
  newDescription: string;
  setNewDescription: (v: string) => void;
  newQuantity: number;
  setNewQuantity: (v: number) => void;
  newUnitPrice: number;
  setNewUnitPrice: (v: number) => void;
  addItem: () => void;
  removeItem: (id: number) => void;
  totalAmount: number;
  estimatedDate: string;
  setEstimatedDate: (v: string) => void;
  sendQuote: (e: React.FormEvent) => void;
  sent: boolean;
}

export function QuoteBuilderView(props: QuoteBuilderViewProps) {
  const {
    workOrder,
    loading,
    error,
    items,
    diagnosisNote,
    setDiagnosisNote,
    startDiagnosis,
    newType,
    setNewType,
    newDescription,
    setNewDescription,
    newQuantity,
    setNewQuantity,
    newUnitPrice,
    setNewUnitPrice,
    addItem,
    removeItem,
    totalAmount,
    estimatedDate,
    setEstimatedDate,
    sendQuote,
    sent,
  } = props;
  const [priceInput, setPriceInput] = useState("");

  if (sent) {
    return (
      <div className={styles.wrap}>
        <div className={styles.success}>
          Đã gửi báo giá cho khách qua email + trong app. Work Order chuyển sang trạng thái &quot;Chờ duyệt giá&quot;.
        </div>
      </div>
    );
  }

  if (loading && !workOrder) {
    return <div className={styles.wrap}>Đang tải work order…</div>;
  }

  if (!workOrder) {
    return <div className={styles.wrap}>{error ?? "Không tìm thấy work order."}</div>;
  }

  // UC-03 bước 1 (docs/02-use-cases.md): WorkOrder mới tiếp nhận đứng ở Received — Technician
  // phải ghi chú nguyên nhân thực tế rồi bấm "Bắt đầu chẩn đoán" trước, chưa được thêm hạng mục
  // báo giá/gửi báo giá (đó là bước sau, khi đã Diagnosing).
  if (workOrder.status === "Received") {
    return (
      <div className={styles.wrap}>
        {error && <div className={styles.error}>{error}</div>}
        <TicketCard code={String(workOrder.id)} onSteel headerRight={<StatusBadge status={workOrder.status} onSteel />}>
          <div className={styles.mono} style={{ color: "#EDEFEE", marginBottom: 12 }}>Xe #{workOrder.vehicleId}</div>
          <div className={styles.sectionLabel}>Ghi chú chẩn đoán</div>
          <textarea
            className={styles.textarea}
            value={diagnosisNote}
            onChange={(e) => setDiagnosisNote(e.target.value)}
            placeholder="Nguyên nhân thực tế sau khi kiểm tra xe…"
          />
        </TicketCard>
        <Button onClick={startDiagnosis} disabled={!diagnosisNote.trim() || loading}>
          {loading ? "Đang chuyển…" : "Bắt đầu chẩn đoán"}
        </Button>
      </div>
    );
  }

  // Đã bắt đầu chẩn đoán (hoặc đã gửi báo giá trước đó) — ghi chú chẩn đoán chỉ sửa được lúc
  // còn ở Received; dùng resend-quote ở trang chi tiết work order để gửi lại báo giá.
  return (
    <form className={styles.wrap} onSubmit={sendQuote}>
      {error && <div className={styles.error}>{error}</div>}

      <TicketCard code={String(workOrder.id)} onSteel headerRight={<StatusBadge status={workOrder.status} onSteel />}>
        <div className={styles.mono} style={{ color: "#EDEFEE", marginBottom: 12 }}>Xe #{workOrder.vehicleId}</div>
        <div className={styles.sectionLabel}>Ghi chú chẩn đoán</div>
        <textarea className={styles.textarea} value={diagnosisNote} readOnly />
      </TicketCard>

      <div>
        <div className={styles.sectionLabel}>Hạng mục báo giá</div>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Loại</th>
              <th>Mô tả</th>
              <th className={styles.right}>SL</th>
              <th className={styles.right}>Đơn giá</th>
              <th className={styles.right}>Thành tiền</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {items.map((item) => (
              <tr key={item.id}>
                <td>{item.type === "Part" ? "Phụ tùng" : "Công"}</td>
                <td>
                  {item.description}
                  {item.lowStockWarning && <span className={styles.warnIcon} title="Tồn kho không đủ, vẫn tạo được báo giá">⚠</span>}
                </td>
                <td className={`${styles.mono} ${styles.right}`}>{item.quantity}</td>
                <td className={`${styles.mono} ${styles.right}`}>{formatCurrency(item.unitPrice)}</td>
                <td className={`${styles.mono} ${styles.right}`}>{formatCurrency(item.lineTotal)}</td>
                <td>
                  <button type="button" className={styles.removeBtn} onClick={() => removeItem(item.id)} disabled={item.isApproved}>
                    Xoá
                  </button>
                </td>
              </tr>
            ))}
            <tr className={styles.newRow}>
              <td>
                <select value={newType} onChange={(e) => setNewType(e.target.value as QuotationItemType)}>
                  <option value="Part">Phụ tùng</option>
                  <option value="Labor">Công</option>
                </select>
              </td>
              <td><input placeholder="Mô tả hạng mục" value={newDescription} onChange={(e) => setNewDescription(e.target.value)} /></td>
              <td><input type="number" min={1} value={newQuantity} onChange={(e) => setNewQuantity(Number(e.target.value))} /></td>
              <td>
                <input
                  placeholder="0"
                  value={priceInput}
                  onChange={(e) => { setPriceInput(e.target.value); setNewUnitPrice(Number(e.target.value) || 0); }}
                />
              </td>
              <td className={`${styles.mono} ${styles.right}`}>{formatCurrency(newQuantity * newUnitPrice)}</td>
              <td>
                <button type="button" className={styles.removeBtn} style={{ color: "var(--safety-amber)" }} onClick={() => { addItem(); setPriceInput(""); }}>
                  + Thêm
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div className={styles.totalBar}>
        <span className={styles.totalLabel}>Tổng tiền</span>
        <span className={styles.totalValue}>{formatCurrency(totalAmount)}</span>
      </div>

      <div className={styles.footer}>
        <input type="date" className={styles.dateInput} value={estimatedDate} onChange={(e) => setEstimatedDate(e.target.value)} />
        <Button type="submit" disabled={items.length === 0 || !estimatedDate || loading}>
          {loading ? "Đang gửi…" : "Gửi báo giá"}
        </Button>
      </div>
    </form>
  );
}
