import Link from "next/link";
import { Button } from "@/components/shared/Button";
import styles from "./AuthCodeView.module.css";

interface ForgotPasswordViewProps {
  email: string;
  setEmail: (v: string) => void;
  message: string | null;
  error: string | null;
  isSubmitting: boolean;
  submit: (e: React.FormEvent) => void;
}

export function ForgotPasswordView({ email, setEmail, message, error, isSubmitting, submit }: ForgotPasswordViewProps) {
  return (
    <div className={styles.page}>
      <div className={styles.card}>
        <div className={styles.logo}>
          GARA<span>CARE</span>
        </div>
        <h1 className={styles.title}>Quên mật khẩu</h1>
        <p className={styles.lede}>Nhập email đã đăng ký — chúng tôi sẽ gửi mã gồm chữ và số để bạn đặt lại mật khẩu.</p>

        {message ? (
          <div className={styles.success}>{message} Đang chuyển sang bước nhập mã…</div>
        ) : (
          <form onSubmit={submit}>
            <div className={styles.field}>
              <label className={styles.label} htmlFor="email">Email</label>
              <input id="email" type="email" className={styles.input} value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="email" />
            </div>
            {error && <p className={styles.errorText}>{error}</p>}
            <Button type="submit" fullWidth disabled={isSubmitting}>
              {isSubmitting ? "Đang gửi…" : "Gửi mã đặt lại mật khẩu"}
            </Button>
          </form>
        )}

        <p className={styles.switchLine}>
          <Link href="/">Quay lại đăng nhập</Link>
        </p>
      </div>
    </div>
  );
}
