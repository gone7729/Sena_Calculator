using GameDamageCalculator.Database;

namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 장신구 모델
    /// </summary>
    public class Accessory
    {
        public int Grade { get; set; }  // 0=없음, 4=4성, 5=5성, 6=6성
        public string MainOption { get; set; }
        public string SubOption { get; set; }  // 6성만

        /// <summary>장신구 종류명(예: "권능의 반지"·"부활의 반지"·"불사의 반지"). null/빈값이면 스탯 전용(이름 없는 옵션 장신구).
        /// 지정 시 GetEffects()로 부활/권능/불사 등 고유효과가 시뮬(SurvivalResolver)에 적용된다.</summary>
        public string RingName { get; set; }

        /// <summary>이 장신구의 고유 효과(부활/권능/불사·보호막 등). RingName이 지정된 경우만 반환(없으면 빈 리스트).</summary>
        public System.Collections.Generic.List<Effects.PersistentEffect> GetEffects()
            => string.IsNullOrEmpty(RingName)
                ? new System.Collections.Generic.List<Effects.PersistentEffect>()
                : AccessoryDb.GetEffects(RingName, Grade);

        /// <summary>기본 공격 시 적에게 상태이상을 거는 반지 효과 — 본 반지(RingName)와 세공 칸 둘 다.
        ///   세공 칸(6성)에는 다른 반지의 효과가 들어간다: 스탯형(치확 10 = 행운의 반지 등)은 SubOption의 옵션 이름으로,
        ///   상태이상형은 SubOption에 반지 이름("샐러맨더의 반지" 등)을 그대로 넣는다. 본 반지와 같은 효과는 중복 불가.
        ///   (2026-11-25 유저 영상: 비스킷 6성 가시 반지 = 기본 공격 시 15% 2턴 출혈 + 세공 = 기본 공격 시 15% 2턴 화상)</summary>
        public System.Collections.Generic.IEnumerable<Effects.PersistentEffect> GetNormalAttackStatusEffects()
        {
            if (Grade <= 0) yield break;
            foreach (var e in GetEffects())
                if (e.Type == Effects.PersistentEffectType.StatusAilment && e.TriggerCondition == Effects.TriggerCondition.NormalOnly) yield return e;
            if (Grade == 6 && !string.IsNullOrEmpty(SubOption) && SubOption != RingName && AccessoryDb.Effects.ContainsKey(SubOption))
                foreach (var e in AccessoryDb.GetEffects(SubOption, Grade))
                    if (e.Type == Effects.PersistentEffectType.StatusAilment && e.TriggerCondition == Effects.TriggerCondition.NormalOnly) yield return e;
        }

        /// <summary>
        /// 장신구 전체 스탯 계산
        /// </summary>
        public BaseStatSet GetTotalStats()
        {
            BaseStatSet stats = new BaseStatSet();

            if (Grade <= 0) return stats;

            // 등급 기본 보너스
            if (AccessoryDb.GradeBonus.TryGetValue(Grade, out var gradeBonus))
                stats.Add(gradeBonus);

            // 메인옵션 — 반지(RingName)는 메인옵 자리를 고유효과가 대신하므로 메인옵 없음
            //   (2026-10 연습전투 실측: 비스킷 권능의 반지 + 부옵 치확10 → 옵션 1줄뿐)
            if (string.IsNullOrEmpty(RingName) && !string.IsNullOrEmpty(MainOption) && MainOption != "없음")
            {
                if (AccessoryDb.MainOptions.TryGetValue(Grade, out var mainOptions))
                {
                    if (mainOptions.TryGetValue(MainOption, out var mainBonus))
                        stats.Add(mainBonus);
                }
            }

            // 부옵션 (6성만)
            if (Grade == 6 && !string.IsNullOrEmpty(SubOption) && SubOption != "없음")
            {
                if (AccessoryDb.SubOptions.TryGetValue(Grade, out var subOptions))
                {
                    if (subOptions.TryGetValue(SubOption, out var subBonus))
                        stats.Add(subBonus);
                }
            }

            // 반지 고유효과 중 "전투 시작 시 피해량 상승"류(토벌·공성·섬멸·복수) — 메인옵 자리를 대신하는 피증 스탯으로 합산.
            //   2026-11-19 영상 실측(토벌의 반지 6성 = 보스 대상 피해 12%): 미반영 시 나타 혼천릉파가 4.7% 낮게 나옴
            //   → 보피증 버킷에 12 가산하면 타격값 일치. 생존·보호막·상태이상 반지는 SurvivalResolver 등이 따로 소비.
            foreach (var eff in GetEffects())
            {
                if (eff.Type != Effects.PersistentEffectType.Buff || eff.Target != Effects.EffectTarget.Self || eff.Buff == null) continue;
                stats.Dmg_Dealt += eff.Buff.Dmg_Dealt;
                stats.Dmg_Dealt_Bos += eff.Buff.Dmg_Dealt_Bos;
                stats.Dmg_Dealt_1to3 += eff.Buff.Dmg_Dealt_1to3;
                stats.Dmg_Dealt_4to5 += eff.Buff.Dmg_Dealt_4to5;
            }

            return stats;
        }

        /// <summary>
        /// 옵션 값 문자열 반환 (UI 표시용)
        /// </summary>
        public static string GetOptionDisplayValue(int grade, string optionName, bool isMainOption)
        {
            if (grade <= 0 || string.IsNullOrEmpty(optionName) || optionName == "없음")
                return "";

            var optionDb = isMainOption ? AccessoryDb.MainOptions : AccessoryDb.SubOptions;

            if (optionDb.TryGetValue(grade, out var options))
            {
                if (options.TryGetValue(optionName, out var stats))
                {
                    if (stats.Atk_Rate > 0) return $"{stats.Atk_Rate}%";
                    if (stats.Def_Rate > 0) return $"{stats.Def_Rate}%";
                    if (stats.Hp_Rate > 0) return $"{stats.Hp_Rate}%";
                    if (stats.Cri > 0) return $"{stats.Cri}%";
                    if (stats.Cri_Dmg > 0) return $"{stats.Cri_Dmg}%";
                    if (stats.Wek > 0) return $"{stats.Wek}%";
                    if (stats.Eff_Hit > 0) return $"{stats.Eff_Hit}%";
                    if (stats.Eff_Res > 0) return $"{stats.Eff_Res}%";
                    if (stats.Blk > 0) return $"{stats.Blk}%";
                    if (stats.Dmg_Dealt > 0) return $"{stats.Dmg_Dealt}%";
                    if (stats.Dmg_Dealt_Bos > 0) return $"{stats.Dmg_Dealt_Bos}%";
                    if (stats.Dmg_Dealt_1to3 > 0) return $"{stats.Dmg_Dealt_1to3}%";
                    if (stats.Dmg_Dealt_4to5 > 0) return $"{stats.Dmg_Dealt_4to5}%";
                }
            }
            return "";
        }
    }
}