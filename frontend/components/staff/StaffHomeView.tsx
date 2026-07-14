// View: thuần hiển thị, nhận toàn bộ dữ liệu/hành động qua props từ ViewModel.
// Không gọi API, không chứa state nghiệp vụ ở đây.
interface StaffHomeViewProps {
  title: string;
}

export function StaffHomeView({ title }: StaffHomeViewProps) {
  return (
    <main>
      <h1>{title}</h1>
    </main>
  );
}
