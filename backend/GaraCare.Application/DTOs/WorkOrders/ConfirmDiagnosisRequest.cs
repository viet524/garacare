using System.ComponentModel.DataAnnotations;

namespace GaraCare.Application.DTOs.WorkOrders;

public class ConfirmDiagnosisRequest
{
    [Required(ErrorMessage = "Ghi chú chẩn đoán là bắt buộc.")]
    public string Notes { get; set; } = string.Empty;

    [Range(0.1, 1000, ErrorMessage = "Số giờ công dự kiến phải lớn hơn 0.")]
    public decimal EstimatedLaborHours { get; set; }
}
