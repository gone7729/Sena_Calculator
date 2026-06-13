"use client";

import { useState } from "react";

// 영웅 아이콘. public/heroes/{id}.png → {이름}.png 순으로 시도, 없으면 플레이스홀더.
// 로드 완료 전·실패 시에는 플레이스홀더만 보여서 깨진 이미지 아이콘이 노출되지 않는다.
export default function HeroIcon({
  id,
  name,
  className,
}: {
  id: number;
  name: string;
  className?: string;
}) {
  const candidates = [`/heroes/${id}.png`, `/heroes/${name}.png`];
  const [idx, setIdx] = useState(0);
  const [loaded, setLoaded] = useState(false);

  const failed = idx >= candidates.length;
  const cls = className ? ` ${className}` : "";

  return (
    <>
      {(!loaded || failed) && <div className={`hero-icon hero-icon-empty${cls}`}>이미지</div>}
      {!failed && (
        <img
          src={candidates[idx]}
          alt={name}
          className={`hero-icon${cls}`}
          style={loaded ? undefined : { display: "none" }}
          onLoad={() => setLoaded(true)}
          onError={() => setIdx((i) => i + 1)}
        />
      )}
    </>
  );
}
