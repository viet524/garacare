import Link from "next/link";

interface HomeViewProps {
  links: { href: string; label: string }[];
}

export function HomeView({ links }: HomeViewProps) {
  return (
    <main>
      <h1>GaraCare</h1>
      <ul>
        {links.map((link) => (
          <li key={link.href}>
            <Link href={link.href}>{link.label}</Link>
          </li>
        ))}
      </ul>
    </main>
  );
}
