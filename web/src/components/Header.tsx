import Link from "next/link";

export default function Header() {
  return (
    <header className="header">
      <Link className="logo" href="/" aria-label="BeHumble 홈으로">
        <span className="logo-mark" aria-hidden="true">
          <span className="logo-mono">bh</span>
        </span>
        <span className="logo-text">
          BeHumble
          <span className="sub">세븐나이츠 리버스 공략</span>
        </span>
      </Link>
      <button className="btn" type="button">
        로그인 / 로그아웃
      </button>
    </header>
  );
}
