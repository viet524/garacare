import styles from "./OnlinePaymentRedirectView.module.css";

export function OnlinePaymentRedirectView() {
  return (
    <div className={styles.wrap}>
      <svg width="64" height="64" viewBox="0 0 64 64" className={styles.spinner}>
        <circle cx="32" cy="32" r="26" fill="none" stroke="rgba(23,24,26,0.14)" strokeWidth="6" />
        <path d="M32 6 A26 26 0 0 1 58 32" fill="none" stroke="var(--safety-amber)" strokeWidth="6" strokeLinecap="round" />
      </svg>
      <p className={styles.text}>Đang chuyển tới cổng thanh toán…</p>
    </div>
  );
}
