import type { Metadata } from "next";
import { Bricolage_Grotesque, Geist, Geist_Mono } from "next/font/google";
import "./globals.css";
import { Providers } from "./providers";
import { Header } from "@/components/Header";
import { ChainHealthBanner } from "@/components/ChainHealthBanner";

const geistSans = Geist({ variable: "--font-geist-sans", subsets: ["latin"] });
const geistMono = Geist_Mono({ variable: "--font-geist-mono", subsets: ["latin"] });
const bricolage = Bricolage_Grotesque({ variable: "--font-bricolage", subsets: ["latin"] });

export const metadata: Metadata = {
  title: "TixFlow",
  description: "Fair-queue event ticketing with NFT tickets on Base",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html
      lang="en"
      className={`${geistSans.variable} ${geistMono.variable} ${bricolage.variable} h-full antialiased`}
    >
      <body className="flex min-h-full flex-col bg-background font-sans text-foreground">
        <Providers>
          <ChainHealthBanner />
          <Header />
          <main className="flex-1">{children}</main>
          <footer className="border-t border-border px-4 py-6 text-center text-xs text-muted">
            TixFlow · Fair-queue ticketing on Base L2
          </footer>
        </Providers>
      </body>
    </html>
  );
}
