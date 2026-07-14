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
  error: string | null;
  isSubmitting: boolean;
  submit: (e: React.FormEvent) => void;
}

export function CustomerRegisterView({ fullName, setFullName, phone, setPhone, email, setEmail, password, setPassword, error, isSubmitting, submit }: CustomerRegisterViewProps) {
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
            <input id="fullName" className={styles.input} value={fullName} onChange={(e) => setFullName(e.target.value)} />
          </div>
          <div className={styles.field}>
            <label className={styles.label} htmlFor="phone">Số điện thoại</label>
            <input id="phone" className={styles.input} value={phone} onChange={(e) => setPhone(e.target.value)} />
          </div>
          <div className={styles.field}>
            <label className={styles.label} htmlFor="email">Email</label>
            <input id="email" className={styles.input} value={email} onChange={(e) => setEmail(e.target.value)} />
          </div>
          <div className={styles.field}>
            <label className={styles.label} htmlFor="password">Mật khẩu</label>
            <input id="password" type="password" className={styles.input} value={password} onChange={(e) => setPassword(e.target.value)} />
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
