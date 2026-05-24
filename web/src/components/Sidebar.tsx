"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

const guideNav = [
  { label: "영웅", href: "/heroes" },
  { label: "레이드", href: "#" },
  { label: "공성전", href: "/siege" },
  { label: "길드전", href: "#" },
];

const communityNav = [
  { label: "팁", href: "#" },
  { label: "이벤트 일정", href: "#" },
  { label: "업데이트", href: "#" },
];

export default function Sidebar() {
  const pathname = usePathname();

  const isActive = (href: string) =>
    href !== "#" && (pathname === href || pathname.startsWith(href + "/"));

  return (
    <aside className="sidebar">
      <div className="nav-section">
        <div className="nav-section-title">공략</div>
        {guideNav.map((item) => (
          <Link
            key={item.label}
            href={item.href}
            className={`nav-item${isActive(item.href) ? " active" : ""}`}
          >
            <span>{item.label}</span>
          </Link>
        ))}
      </div>

      <div className="nav-section">
        <div className="nav-section-title">커뮤니티</div>
        {communityNav.map((item) => (
          <Link
            key={item.label}
            href={item.href}
            className={`nav-item${isActive(item.href) ? " active" : ""}`}
          >
            <span>{item.label}</span>
          </Link>
        ))}
      </div>
    </aside>
  );
}
