import Link from "next/link";
import { Button } from "@/components/shared/Button";
import { ProgressGauge } from "@/components/shared/ProgressGauge";
import styles from "./LoginView.module.css";

interface DemoAccount { username: string; role: string; label: string }

interface LoginViewProps {
  username: string;
  setUsername: (v: string) => void;
  password: string;
  setPassword: (v: string) => void;
  error: string | null;
  isSubmitting: boolean;
  submit: (e: React.FormEvent) => void;
  demoAccounts: DemoAccount[];
}

// Điểm đăng nhập duy nhất cho toàn hệ thống — Customer và nội bộ (Staff/Technician/Admin)
// dùng chung 1 form, điều hướng sau đăng nhập dựa theo Role trả về từ backend.
export function LoginView({ username, setUsername, password, setPassword, error, isSubmitting, submit, demoAccounts }: LoginViewProps) {
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
            <label className={styles.label} htmlFor="username">Tên đăng nhập</label>
            <input id="username" className={styles.input} value={username} onChange={(e) => setUsername(e.target.value)} autoComplete="username" />
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
            Chưa có tài khoản khách hàng? <Link href="/customer/register" style={{ color: "var(--safety-amber)", fontWeight: 600 }}>Đăng ký</Link>
          </p>

          <div className={styles.demoBox}>
            <div className={styles.demoLabel}>Tài khoản demo (bấm để điền)</div>
            {demoAccounts.map((acc) => (
              <button
                type="button"
                key={acc.username}
                className={styles.demoRow}
                onClick={() => { setUsername(acc.username); setPassword("demo"); }}
              >
                <span>{acc.label}</span>
                <span>{acc.username}</span>
              </button>
            ))}
          </div>
        </form>
      </div>
    </div>
  );
}
