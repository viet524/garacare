import Link from "next/link";
import { Button } from "@/components/shared/Button";
import { ProgressGauge } from "@/components/shared/ProgressGauge";
import styles from "./AuthSplitView.module.css";

interface CustomerRegisterViewProps {
  fullName: string;
  setFullName: (v: string) => void;
  phone: string;
  setPhone: (v: string) => void;
  email: string;
  setEmail: (v: string) => void;
  password: string;
  setPassword: (v: string) => void;
  confirmPassword: string;
  setConfirmPassword: (v: string) => void;
  error: string | null;
  fieldErrors: Record<string, string>;
  isSubmitting: boolean;
  submit: (e: React.FormEvent) => void;
}

export function CustomerRegisterView({
  fullName,
  setFullName,
  phone,
  setPhone,
  email,
  setEmail,
  password,
  setPassword,
  confirmPassword,
  setConfirmPassword,
  error,
  fieldErrors,
  isSubmitting,
  submit,
}: CustomerRegisterViewProps) {
  return (
    <div className={styles.split}>
      <div className={styles.brand}>
        <div className={styles.brandLogo}>
          GARA<span>CARE</span>
        </div>
        <p className={styles.brandTagline}>Tạo tài khoản để đặt lịch, duyệt báo giá và theo dõi tiến độ mọi lúc.</p>
        <ProgressGauge status="Delivered" onSteel size={200} />
      </div>
      <div className={styles.formSide}>
        <form className={styles.formCard} onSubmit={submit}>
          <div className={styles.field}>
            <label className={styles.label} htmlFor="fullName">Họ và tên</label>
            <input id="fullName" className={`${styles.input} ${fieldErrors.fullName ? styles.inputError : ""}`} value={fullName} onChange={(e) => setFullName(e.target.value)} />
            {fieldErrors.fullName && <p className={styles.fieldError}>{fieldErrors.fullName}</p>}
          </div>
          <div className={styles.field}>
            <label className={styles.label} htmlFor="phone">Số điện thoại</label>
            <input id="phone" className={`${styles.input} ${fieldErrors.phone ? styles.inputError : ""}`} value={phone} onChange={(e) => setPhone(e.target.value)} />
            {fieldErrors.phone && <p className={styles.fieldError}>{fieldErrors.phone}</p>}
          </div>
          <div className={styles.field}>
            <label className={styles.label} htmlFor="email">Email</label>
            <input id="email" type="email" className={`${styles.input} ${fieldErrors.email ? styles.inputError : ""}`} value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="email" />
            {fieldErrors.email && <p className={styles.fieldError}>{fieldErrors.email}</p>}
          </div>
          <div className={styles.field}>
            <label className={styles.label} htmlFor="password">Mật khẩu</label>
            <input id="password" type="password" className={`${styles.input} ${fieldErrors.password ? styles.inputError : ""}`} value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="new-password" />
            {fieldErrors.password && <p className={styles.fieldError}>{fieldErrors.password}</p>}
          </div>
          <div className={styles.field}>
            <label className={styles.label} htmlFor="confirmPassword">Xác nhận mật khẩu</label>
            <input id="confirmPassword" type="password" className={`${styles.input} ${fieldErrors.confirmPassword ? styles.inputError : ""}`} value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} autoComplete="new-password" />
            {fieldErrors.confirmPassword && <p className={styles.fieldError}>{fieldErrors.confirmPassword}</p>}
          </div>
          {error && <p className={styles.errorText}>{error}</p>}
          <Button type="submit" fullWidth disabled={isSubmitting}>
            {isSubmitting ? "Đang tạo tài khoản…" : "Đăng ký"}
          </Button>
          <p className={styles.switchLine}>
            Đã có tài khoản? <Link href="/">Đăng nhập</Link>
          </p>
        </form>
      </div>
    </div>
  );
}
