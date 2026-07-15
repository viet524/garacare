import { Button } from "@/components/shared/Button";
import styles from "./AuthCodeView.module.css";

interface VerifyEmailViewProps {
  email: string;
  setEmail: (v: string) => void;
  code: string;
  setCode: (v: string) => void;
  error: string | null;
  fieldErrors: Record<string, string>;
  info: string | null;
  isSubmitting: boolean;
  isResending: boolean;
  submit: (e: React.FormEvent) => void;
  resend: () => void;
}

export function VerifyEmailView({ email, setEmail, code, setCode, error, fieldErrors, info, isSubmitting, isResending, submit, resend }: VerifyEmailViewProps) {
  return (
    <div className={styles.page}>
      <div className={styles.card}>
        <div className={styles.logo}>
          GARA<span>CARE</span>
        </div>
        <h1 className={styles.title}>Xác minh tài khoản</h1>
        <p className={styles.lede}>Chúng tôi đã gửi mã gồm chữ và số tới email đăng ký. Nhập mã bên dưới để kích hoạt tài khoản.</p>

        <form onSubmit={submit}>
          <div className={styles.field}>
            <label className={styles.label} htmlFor="email">Email</label>
            <input id="email" type="email" className={`${styles.input} ${fieldErrors.email ? styles.inputError : ""}`} value={email} onChange={(e) => setEmail(e.target.value)} />
            {fieldErrors.email && <p className={styles.fieldError}>{fieldErrors.email}</p>}
          </div>
          <div className={styles.field}>
            <label className={styles.label} htmlFor="code">Mã xác minh</label>
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
          {error && <p className={styles.errorText}>{error}</p>}
          {info && <p className={styles.infoText}>{info}</p>}
          <Button type="submit" fullWidth disabled={isSubmitting}>
            {isSubmitting ? "Đang xác minh…" : "Xác minh tài khoản"}
          </Button>
        </form>

        <button type="button" className={styles.linkBtn} onClick={resend} disabled={isResending}>
          {isResending ? "Đang gửi lại…" : "Gửi lại mã xác minh"}
        </button>
      </div>
    </div>
  );
}
