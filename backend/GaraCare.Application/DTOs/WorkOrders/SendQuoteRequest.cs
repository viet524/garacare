using System.ComponentModel.DataAnnotations;

namespace GaraCare.Application.DTOs.WorkOrders;

public class SendQuoteRequest
{
    [Required(ErrorMessage = "Ngày dự kiến hoàn thành là bắt buộc.")]
    public DateTime EstimatedCompletionDate { get; set; }
}
