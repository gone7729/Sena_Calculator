import type { Metadata } from "next";
import "./globals.css";
import Sidebar from "@/components/Sidebar";
import Header from "@/components/Header";

export const metadata: Metadata = {
  title: "BeHumble · 세븐나이츠 리버스 공략",
  description: "길드원을 위한 세븐나이츠 리버스 내부 공략 사이트",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="ko">
      <body>
        <div className="app">
          <Sidebar />
          <div className="content">
            <Header />
            {children}
          </div>
        </div>
      </body>
    </html>
  );
}
