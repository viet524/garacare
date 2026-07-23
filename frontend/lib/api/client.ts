import { API_BASE_URL } from "./config";
import { clearSession, getSession, saveSession, type Session } from "@/lib/auth/session";

export class ApiError extends Error {
  status: number;
  // Chỉ có giá trị khi backend trả ValidationProblemDetails (ModelState tự validate của
  // [ApiController]) — key trùng tên property C# (PascalCase, vd "Email", "FullName").
  fieldErrors?: Record<string, string>;

  constructor(status: number, message: string, fieldErrors?: Record<string, string>) {
    super(message);
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}

// "Email" -> "email", "FullName" -> "fullName" — khớp tên field camelCase dùng trong viewmodel.
function toCamelCase(key: string): string {
  return key.charAt(0).toLowerCase() + key.slice(1);
}

// Đọc field-level error để tô đỏ đúng ô nhập + hiện lỗi ngay dưới ô đó, thay vì chỉ 1 dòng
// thông báo chung chung ở cuối form.
export function getFieldErrors(err: unknown): Record<string, string> {
  if (err instanceof ApiError && err.fieldErrors) {
    return err.fieldErrors;
  }
  return {};
}

interface RequestOptions extends Omit<RequestInit, "body"> {
  body?: unknown;
  token?: string;
}

// Backend trả lỗi ở 2 dạng khác nhau tuỳ nguồn gốc lỗi:
// - Exception nghiệp vụ (ExceptionHandlingMiddleware): { message: "..." }
// - ModelState tự validate của [ApiController] (thiếu field [Required], sai [EmailAddress]...):
//   ValidationProblemDetails chuẩn ASP.NET Core, dạng { title, errors: { Field: ["msg", ...] } },
//   KHÔNG có field "message" — nếu không xử lý riêng, lỗi này sẽ hiện chung chung
//   "Request failed with status 400" thay vì nói rõ field nào thiếu.
function extractFieldErrors(data: unknown): Record<string, string> | undefined {
  if (data && typeof data === "object") {
    const obj = data as Record<string, unknown>;
    if (obj.errors && typeof obj.errors === "object") {
      const result: Record<string, string> = {};
      for (const [key, messages] of Object.entries(obj.errors as Record<string, string[]>)) {
        if (messages.length > 0) {
          result[toCamelCase(key)] = messages[0];
        }
      }
      return Object.keys(result).length > 0 ? result : undefined;
    }
  }
  return undefined;
}

// Không hiện mã lỗi HTTP thô (401/403/500...) ra UI — vừa không thân thiện với người dùng,
// vừa cho kẻ tấn công manh mối để dò lỗi hệ thống. Map theo nhóm mã thành câu chữ chung chung.
function defaultMessageForStatus(status: number): string {
  if (status === 401) return "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại.";
  if (status === 403) return "Bạn không có quyền thực hiện thao tác này.";
  if (status === 404) return "Không tìm thấy dữ liệu yêu cầu.";
  if (status >= 500) return "Đã có lỗi xảy ra ở hệ thống, vui lòng thử lại sau.";
  return "Yêu cầu không thực hiện được, vui lòng kiểm tra lại thông tin đã nhập.";
}

function extractErrorMessage(data: unknown, status: number): string {
  if (data && typeof data === "object") {
    const obj = data as Record<string, unknown>;
    if (typeof obj.message === "string" && obj.message.trim()) {
      return obj.message;
    }
    if (obj.errors && typeof obj.errors === "object") {
      const messages = Object.values(obj.errors as Record<string, string[]>).flat();
      if (messages.length > 0) {
        return messages.join(" ");
      }
    }
    if (typeof obj.title === "string" && obj.title.trim()) {
      return obj.title;
    }
  }
  return defaultMessageForStatus(status);
}

async function rawFetch<T>(path: string, options: RequestOptions): Promise<T> {
  const { body, token, headers, ...rest } = options;

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...rest,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...headers,
    },
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  if (!response.ok) {
    const data = await response.json().catch(() => null);
    throw new ApiError(response.status, extractErrorMessage(data, response.status), extractFieldErrors(data));
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

// Access token sống rất ngắn (xem docs/03-data-model.md — RefreshToken). Nhiều request có thể
// cùng lúc gặp 401 vì token vừa hết hạn — gộp lại thành 1 lần gọi /auth/refresh-token duy nhất
// thay vì mỗi request tự refresh riêng (tránh refresh token bị rotate nhiều lần cùng lúc, dẫn
// tới các request refresh sau bị từ chối vì token đã bị request refresh trước đó thu hồi).
let refreshInFlight: Promise<string | null> | null = null;

async function refreshAccessToken(currentRefreshToken: string): Promise<string | null> {
  if (!refreshInFlight) {
    refreshInFlight = rawFetch<{ token: string; refreshToken: string; role: Session["role"]; userId: number; fullName: string }>(
      "/api/auth/refresh-token",
      { method: "POST", body: { refreshToken: currentRefreshToken } },
    )
      .then((result) => {
        saveSession({
          token: result.token,
          refreshToken: result.refreshToken,
          role: result.role,
          userId: result.userId,
          fullName: result.fullName,
        });
        return result.token;
      })
      .catch(() => {
        clearSession();
        return null;
      })
      .finally(() => {
        refreshInFlight = null;
      });
  }
  return refreshInFlight;
}

// Wrapper fetch dùng chung cho mọi file trong /lib/api/<resource>.ts.
// Không gọi fetch() rải rác trong component (xem docs/08-frontend-conventions.md).
export async function apiFetch<T>(path: string, options: RequestOptions = {}): Promise<T> {
  try {
    return await rawFetch<T>(path, options);
  } catch (err) {
    // Access token hết hạn — thử refresh 1 lần bằng refresh token đang lưu rồi gọi lại request
    // gốc. Bỏ qua chính các endpoint /api/auth/* để không tạo vòng lặp refresh-vô-hạn.
    const isAuthEndpoint = path.startsWith("/api/auth/");
    if (err instanceof ApiError && err.status === 401 && options.token && !isAuthEndpoint) {
      const session = getSession();
      if (session?.refreshToken) {
        const newToken = await refreshAccessToken(session.refreshToken);
        if (newToken) {
          return rawFetch<T>(path, { ...options, token: newToken });
        }
      }
      // Refresh token cũng hết hạn/bị thu hồi — session đã bị clearSession() ở refreshAccessToken,
      // chỉ còn cách bắt đăng nhập lại.
      if (typeof window !== "undefined") {
        window.location.href = "/";
      }
    }
    throw err;
  }
}
