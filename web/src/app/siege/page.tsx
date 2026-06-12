"use client";

import { useState } from "react";
import SimViewer from "./SimViewer";
import CustomViewer from "./CustomViewer";

export default function SiegePage() {
  // 상단 뷰 전환: 시뮬(요일별 정식 추천 빌드 뷰어) / 커스텀 뷰어(영웅 직접 선택·탐색)
  const [view, setView] = useState<"sim" | "custom">("sim");

  return (
    <>
      {/* ===== 제목 + 뷰 탭(우상단) ===== */}
      <div className="siege-header">
        <h1 className="page-title">공성전</h1>
        <div className="siege-mode-toggle">
          <button
            type="button"
            className={`chip${view === "sim" ? " active" : ""}`}
            onClick={() => setView("sim")}
            title="요일별 정식 추천 빌드 보기"
          >
            시뮬
          </button>
          <button
            type="button"
            className={`chip${view === "custom" ? " active" : ""}`}
            onClick={() => setView("custom")}
            title="영웅을 직접 선택해 세팅·스킬순서 탐색"
          >
            커스텀 뷰어
          </button>
        </div>
      </div>

      {view === "sim" ? <SimViewer /> : <CustomViewer />}
    </>
  );
}
