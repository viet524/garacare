import type { ButtonHTMLAttributes } from "react";
import styles from "./Button.module.css";

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "primary" | "secondary" | "danger";
  fullWidth?: boolean;
  onSteel?: boolean;
}

export function Button({ variant = "primary", fullWidth = false, onSteel = false, className, ...rest }: ButtonProps) {
  return (
    <button
      className={`${styles.btn} ${styles[variant]} ${fullWidth ? styles.full : ""} ${onSteel ? styles.onSteel : ""} ${className ?? ""}`}
      {...rest}
    />
  );
}
