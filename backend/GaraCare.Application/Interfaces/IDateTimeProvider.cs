namespace GaraCare.Application.Interfaces;

// Singleton pattern: một nguồn "thời gian hiện tại" duy nhất dùng chung toàn app,
// thay vì gọi DateTime.UtcNow rải rác — giúp mock được trong unit test (ví dụ rule
// "QuotePending quá 24h chưa duyệt" ở docs/06-workflow-rules.md).
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
