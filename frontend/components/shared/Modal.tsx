import type { ReactNode } from "react";
import styles from "./Modal.module.css";

interface ModalProps {
  title: string;
  onClose: () => void;
  children: ReactNode;
  theme?: "dark" | "light";
}

export function Modal({ title, onClose, children, theme = "dark" }: ModalProps) {
  return (
    <div className={styles.overlay} onClick={onClose}>
      <div className={`${styles.panel} ${theme === "dark" ? styles.panelDark : styles.panelLight}`} onClick={(e) => e.stopPropagation()}>
        <div className={styles.header}>
          <div className={styles.title}>{title}</div>
          <button type="button" className={styles.closeBtn} onClick={onClose}>Đóng ✕</button>
        </div>
        {children}
      </div>
    </div>
  );
}
