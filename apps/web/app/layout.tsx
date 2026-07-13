import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "ONE. Competitions",
  description: "Multi-tenant competition and giveaway platform foundation"
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
