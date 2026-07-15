import { Button } from "@/components/shared/Button";
import styles from "./AuthCodeView.module.css";

interface ResetPasswordViewProps {
  email: string;
  setEmail: (v: string) => void;
  code: string;
  setCode: (v: string) => void;
  newPassword: string;
  setNewPassword: (v: string) => void;
  confirmNewPassword: string;
  setConfirmNewPassword: (v: string) => void;
  error: string | null;
  fieldErrors: Record<string, string>;
  isSubmitting: boolean;
  done: boolean;
  submit: (e: React.FormEvent) => void;
}

export function ResetPasswordView({
  email,
  setEmail,
  code,
  setCode,
  newPassword,
  setNewPassword,
  confirmNewPassword,
  setConfirmNewPassword,
  error,
  fieldErrors,
  isSubmitting,
  done,
  submit,
}: ResetPasswordViewProps) {
  return (
    <div className={styles.page}>
      <div className={styles.card}>
        <div className={styles.logo}>
          GARA<span>CARE</span>
        </div>
        <h1 className={styles.title}>Đặt lại mật khẩu</h1>
        <p className={styles.lede}>Nhập mã gồm chữ và số đã gửi tới email, cùng mật khẩu mới.</p>

        {done ? (
          <div className={styles.success}>Đặt lại mật khẩu thành công. Đang chuyển sang trang đăng nhập…</div>
        ) : (
          <form onSubmit={submit}>
            <div className={styles.field}>
              <label className={styles.label} htmlFor="email">Email</label>
              <input id="email" type="email" className={`${styles.input} ${fieldErrors.email ? styles.inputError : ""}`} value={email} onChange={(e) => setEmail(e.target.value)} />
              {fieldErrors.email && <p className={styles.fieldError}>{fieldErrors.email}</p>}
            </div>
            <div className={styles.field}>
              <label className={styles.label} htmlFor="code">Mã đặt lại mật khẩu</label>
              <input
                id="code"
                className={`${styles.input} ${styles.codeInput} ${fieldErrors.code ? styles.inputError : ""}`}
                value={code}
                onChange={(e) => setCode(e.target.value)}
                maxLength={6}
                placeholder="AB12CD"
              />
              {fieldErrors.code && <p className={styles.fieldError}>{fieldErrors.code}</p>}
            </div>
            <div className={styles.field}>
              <label className={styles.label} htmlFor="newPassword">Mật khẩu mới</label>
              <input id="newPassword" type="password" className={`${styles.input} ${fieldErrors.newPassword ? styles.inputError : ""}`} value={newPassword} onChange={(e) => setNewPassword(e.target.value)} autoComplete="new-password" />
              {fieldErrors.newPassword && <p className={styles.fieldError}>{fieldErrors.newPassword}</p>}
            </div>
            <div className={styles.field}>
              <label className={styles.label} htmlFor="confirmNewPassword">Xác nhận mật khẩu mới</label>
              <input id="confirmNewPassword" type="password" className={`${styles.input} ${fieldErrors.confirmNewPassword ? styles.inputError : ""}`} value={confirmNewPassword} onChange={(e) => setConfirmNewPassword(e.target.value)} autoComplete="new-password" />
              {fieldErrors.confirmNewPassword && <p className={styles.fieldError}>{fieldErrors.confirmNewPassword}</p>}
            </div>
            {error && <p className={styles.errorText}>{error}</p>}
            <Button type="submit" fullWidth disabled={isSubmitting}>
              {isSubmitting ? "Đang đặt lại…" : "Đặt lại mật khẩu"}
            </Button>
          </form>
        )}
      </div>
    </div>
  );
}
