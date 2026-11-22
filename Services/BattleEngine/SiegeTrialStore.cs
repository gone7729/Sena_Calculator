using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Services.BattleEngine
{
    // ============================================================================
    // 탐색 시도 기록 (2026-11-22 유저 방침: 대조 자료 없이 "도태된 시도"에서 누수를 찾는다).
    //   SiegeBuildStore는 최고 빌드 하나만 남긴다 — 도태된 후보는 점수조차 남지 않아 "왜 졌는지"를 볼 수 없었다.
    //   여기서는 탐색이 평가한 완성 후보(SiegeOptimizerConfig.OnTrial)와 각 단계의 채택 빌드(OnStage)를
    //   한 줄에 하나씩(JSONL) 덧붙여 쌓는다. 한 줄 = 빌드 전체(장비·자리·로테 → 언제든 재생 가능)
    //   + 점수·사망·영웅별 딜 + 실제로 실행된 스킬 순서(시점·쿨 여유·버프/디버프 대상).
    //   파일: results/siege/trials_{요일}_{프로필}_{팀}.jsonl — 지우지 않고 계속 덧붙인다.
    // ============================================================================
    public class SiegeTrialRecord
    {
        public int Version { get; set; } = 1;
        public string At { get; set; }
        public string Day { get; set; }
        public string Profile { get; set; }
        public string TeamKey { get; set; }
        public string StartOrder { get; set; }      // 이 시도가 나온 탐색의 출발 순서
        public string Phase { get; set; }           // 탐색 단계 (기본 탐색 / 공격 로테+생존 보강 / 공격력 순위 / 행동 순서 / 속공 선행 …)
        public int Seq { get; set; }                // 그 탐색(출발 순서) 안에서의 순번 — 시간 순
        public bool Adopted { get; set; }           // true = 그 단계가 끝났을 때 살아남은 빌드(OnStage). false = 평가된 후보(채택 여부 무관)
        public string EngineHash { get; set; }
        public string GitCommit { get; set; }

        // 버프·쿨감 수령자 강제(공격력 순위 탐색의 "만약 이 순서로 받는다면" 가정 평가). null이 아니면 이 시도의 점수는
        //   빌드만으로는 재현되지 않는다 — 재생할 때 SiegeOptimizerConfig.HighestAtkAllyOrder 를 같은 순서로 줘야 한다.
        public List<string> ForcedBuffOrder { get; set; }
        public bool ForcedCdrOnly { get; set; }

        public double Score { get; set; }           // 실제 딜(TotalScore)
        public double? ReplayScore { get; set; }    // 스킬 순서를 얻으려고 시드 777로 한 번 더 돌린 점수(단일 시드 요일이면 Score와 같아야 함)
        public double RankScore { get; set; }       // Score − Penalty × 사망 수 (탐색의 채택 기준)
        public double Penalty { get; set; }
        public double DeathCount { get; set; }      // N시드 평가 요일은 시드 평균(소수 가능)
        public List<DeathDto> Deaths { get; set; } = new();
        public int RoundsCleared { get; set; }
        public int TotalTurns { get; set; }
        public double ElapsedSeconds { get; set; }

        public string Formation { get; set; }
        public List<string> BackRow { get; set; } = new();
        public List<string> Order { get; set; } = new();     // 진형 번호 순(=같은 속공일 때 행동 순서)
        public Dictionary<string, double> Damage { get; set; } = new();   // 영웅별 딜

        public List<CastDto> Casts { get; set; } = new();    // 실제로 실행된 스킬 턴 순서(시드 777 재생)
        public string Fingerprint { get; set; }              // 빌드(자리·장비·로테) 지문 — 같은 빌드 중복 판별
        public SiegeSavedBuild Build { get; set; }            // 빌드 전체 — SiegeBuildStore.ToParty/ToPlan 으로 재생

        public class DeathDto
        {
            public string Name { get; set; }
            public int Turn { get; set; }
            public string Cause { get; set; }
        }

        public class CastDto
        {
            public int Step { get; set; }           // 아군 스킬 턴 순번(0부터)
            public int Turn { get; set; }
            public double Sec { get; set; }          // 경과 게임 시간(초)
            public string Hero { get; set; }
            public string Skill { get; set; }
            public string Status { get; set; }       // 첫시전 / 재시전 / 폴백 / 자동 / 홀드
            public double? Slack { get; set; }       // 재시전: 쿨이 돈 뒤 얼마나 지나 썼는지(초) — 클수록 쿨을 놀린 것
            public double? CooldownLeft { get; set; }// 폴백: 계획한 스킬의 남은 쿨(초)
            public string Fallback { get; set; }     // 폴백 사유
            public List<string> BuffTargets { get; set; }
            public List<string> DebuffTargets { get; set; }
            public List<string> DispelTargets { get; set; }
        }
    }

    public static class SiegeTrialStore
    {
        private static readonly JsonSerializerOptions Opts = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        /// <summary>빌드 지문 — 진형·후열·자리 순서·영웅별 장비/장신구/전용장비·로테가 모두 같으면 같은 값.</summary>
        public static string Fingerprint(SiegeSavedBuild b)
        {
            var sb = new StringBuilder();
            sb.Append(b.Formation).Append('|').Append(string.Join(",", b.BackRow.OrderBy(x => x, StringComparer.Ordinal))).Append('|');
            foreach (var h in b.Heroes.OrderBy(h => h.Slot))
            {
                sb.Append(h.Slot).Append(':').Append(h.Id).Append(h.Awakened ? "A" : "").Append('t').Append(h.Transcend).Append('[');
                foreach (var g in h.Gear)
                    sb.Append(g.Slot).Append('/').Append(g.Set).Append('/').Append(g.Main).Append('/').Append(g.Enhance).Append('/')
                      .Append(string.Join(",", g.Subs.Select(x => $"{x.Stat}{x.Tier}"))).Append(';');
                if (h.Accessory != null)
                    sb.Append("acc/").Append(h.Accessory.Grade).Append('/').Append(h.Accessory.Ring).Append('/').Append(h.Accessory.Main).Append('/').Append(h.Accessory.Sub).Append(';');
                if (h.Exclusive != null)
                    sb.Append("ex/").Append(string.Join(",", h.Exclusive.Tuning.Select(t => $"{t.Option}{t.Grade}"))).Append(';');
                sb.Append(']');
            }
            sb.Append('|');
            foreach (var s in b.Rotation) sb.Append(s.Hold ? "-" : $"{s.Hero}:{s.Skill}").Append(',');
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()))).ToLowerInvariant()[..16];
        }

        /// <summary>시도 1건을 만든다. team은 진형 번호 순이고 IsBackPosition이 그 후보의 자리와 맞아야 한다.
        ///   result = 탐색이 낸 점수(N시드 평균 포함), replay = 같은 빌드를 Feasibility 기록을 켜고 한 번 돌린 결과(스킬 순서용, null 가능).</summary>
        public static SiegeTrialRecord Make(List<BattleCharacter> team, string formation, List<RotationDecision> plan,
            SiegeBattleResult result, SiegeBattleResult replay, double penalty)
        {
            var back = team.Where(p => p.IsBackPosition).Select(p => p.Character.Name).ToList();
            var build = SiegeBuildStore.Capture(team, formation, back, plan);
            double deaths = result.EvalSeedCount > 1 && result.DeathSeedCounts != null
                ? result.DeathSeedCounts.Values.Sum() / (double)result.EvalSeedCount
                : result.Deaths?.Count ?? 0;
            var rec = new SiegeTrialRecord
            {
                Score = Math.Round(result.TotalScore), Penalty = penalty, DeathCount = deaths,
                RankScore = Math.Round(result.TotalScore - penalty * deaths),
                Deaths = (result.Deaths ?? new()).Select(d => new SiegeTrialRecord.DeathDto { Name = d.AllyName, Turn = d.Turn, Cause = d.Cause }).ToList(),
                RoundsCleared = result.RoundsCleared, TotalTurns = result.TotalTurns, ElapsedSeconds = Math.Round(result.ElapsedSeconds, 1),
                Formation = formation, BackRow = back, Order = team.Select(p => p.Character.Name).ToList(),
                Damage = (result.CharacterResults ?? new()).ToDictionary(c => c.CharacterName, c => Math.Round(c.TotalDamage)),
                Fingerprint = Fingerprint(build), Build = build,
                ReplayScore = replay == null ? null : Math.Round(replay.TotalScore),
            };
            if (replay?.Feasibility != null)
                foreach (var f in replay.Feasibility.Where(f => f.Reached))
                    rec.Casts.Add(new SiegeTrialRecord.CastDto
                    {
                        Step = f.StepIndex, Turn = f.Turn, Sec = Math.Round(f.Elapsed, 1),
                        Hero = f.Hold ? null : f.HeroName, Skill = f.Hold ? null : f.SkillName,
                        Status = f.Hold ? "홀드" : f.IsAuto ? "자동" : !f.ExecutedAsPlanned ? "폴백" : f.CooldownGated ? "재시전" : "첫시전",
                        Slack = !f.Hold && !f.IsAuto && f.ExecutedAsPlanned && f.CooldownGated ? Math.Round(f.Slack, 1) : null,
                        CooldownLeft = !f.Hold && !f.ExecutedAsPlanned && f.CooldownRemaining > 0 ? Math.Round(f.CooldownRemaining, 1) : null,
                        Fallback = !f.Hold && !f.ExecutedAsPlanned ? f.FallbackReason : null,
                        BuffTargets = f.BuffTargets is { Count: > 0 } ? f.BuffTargets.ToList() : null,
                        DebuffTargets = f.DebuffTargets is { Count: > 0 } ? f.DebuffTargets.ToList() : null,
                        DispelTargets = f.DispelTargets is { Count: > 0 } ? f.DispelTargets.ToList() : null,
                    });
            return rec;
        }

        public static void Append(string path, SiegeTrialRecord rec)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.AppendAllText(path, JsonSerializer.Serialize(rec, Opts) + "\n", new UTF8Encoding(false));
        }

        /// <summary>파일의 모든 시도(읽지 못하는 줄은 건너뜀). 없으면 빈 목록.</summary>
        public static List<SiegeTrialRecord> Load(string path)
        {
            var list = new List<SiegeTrialRecord>();
            if (!File.Exists(path)) return list;
            foreach (var line in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try { var r = JsonSerializer.Deserialize<SiegeTrialRecord>(line, Opts); if (r != null) list.Add(r); }
                catch { }
            }
            return list;
        }
    }
}
