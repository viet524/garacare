interface CustomerHomeViewProps {
  title: string;
}

export function CustomerHomeView({ title }: CustomerHomeViewProps) {
  return (
    <main>
      <h1>{title}</h1>
    </main>
  );
}
