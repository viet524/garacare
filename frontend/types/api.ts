// Type ở đây phải khớp 1-1 với Response DTO backend (GaraCare.Application/DTOs),
// không phải Entity. Cập nhật cùng lúc khi DTO backend thay đổi.
// Hiện DTO backend chưa được định nghĩa cho từng resource — thêm type tương ứng
// khi implement từng use case, không tự bịa field trước.

export interface ApiErrorResponse {
  message: string;
}
