import Link from "next/link";
import { Button } from "@/components/shared/Button";
import { ProgressGauge } from "@/components/shared/ProgressGauge";
import styles from "./LoginView.module.css";

interface LoginViewProps {
  email: string;
  setEmail: (v: string) => void;
  password: string;
  setPassword: (v: string) => void;
  error: string | null;
  isSubmitting: boolean;
  submit: (e: React.FormEvent) => void;
}

// Điểm đăng nhập duy nhất cho toàn hệ thống — Customer và nội bộ (Staff/Technician/Admin)
// dùng chung 1 form (Email + mật khẩu), điều hướng sau đăng nhập dựa theo Role trả về từ backend.
export function LoginView({ email, setEmail, password, setPassword, error, isSubmitting, submit }: LoginViewProps) {
  return (
    <div className={styles.split}>
      <div className={styles.brand}>
        <div className={styles.brandLogo}>
          GARA<span>CARE</span>
        </div>
        <p className={styles.brandTagline}>Một cổng đăng nhập duy nhất — khách hàng theo dõi xe của mình, lễ tân/kỹ thuật viên quản lý toàn bộ xưởng.</p>
        <ProgressGauge status="InRepair" onSteel size={200} />
      </div>
      <div className={styles.formSide}>
        <form className={styles.formCard} onSubmit={submit}>
          <div className={styles.field}>
            <label className={styles.label} htmlFor="email">Email</label>
            <input id="email" type="email" className={styles.input} value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="email" />
          </div>
          <div className={styles.field}>
            <label className={styles.label} htmlFor="password">Mật khẩu</label>
            <input id="password" type="password" className={styles.input} value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="current-password" />
          </div>
          {error && <p className={styles.errorText}>{error}</p>}
          <Button type="submit" fullWidth disabled={isSubmitting}>
            {isSubmitting ? "Đang đăng nhập…" : "Đăng nhập"}
          </Button>
          <p style={{ textAlign: "center", fontSize: 13, marginTop: 16 }}>
            <Link href="/forgot-password" style={{ color: "var(--safety-amber)", fontWeight: 600 }}>Quên mật khẩu?</Link>
          </p>
          <p style={{ textAlign: "center", fontSize: 13, marginTop: 8 }}>
            Chưa có tài khoản khách hàng? <Link href="/register" style={{ color: "var(--safety-amber)", fontWeight: 600 }}>Đăng ký</Link>
          </p>
        </form>
      </div>
    </div>
  );
}
