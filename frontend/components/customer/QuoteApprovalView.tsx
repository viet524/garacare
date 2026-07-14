interface QuoteApprovalViewProps {
  token: string;
}

export function QuoteApprovalView({ token }: QuoteApprovalViewProps) {
  return (
    <main>
      <h1>Duyệt báo giá</h1>
      <p>Token: {token}</p>
    </main>
  );
}
