using System.Collections.Concurrent;
using System.Threading;
using System.Text.Json.Serialization;
using GameDamageCalculator.Database;
using GameDamageCalculator.Models;
using GameDamageCalculator.Services.BattleEngine;

// 공성전 웹(/siege) 백엔드. SiegeOptimizer를 HTTP로 노출.
var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    o.SerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
});

// 로컬 개발: Next.js dev(3000) 등 임의 출처 허용
const string CorsPolicy = "web";
builder.Services.AddCors(o => o.AddPolicy(CorsPolicy, p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors(CorsPolicy);

// 프리필 대상 추천 팀 (요일 → 팀들[영웅 id]). 캐시를 미리 채워 유저에게 즉시 표시.
//   필요 시 요일·팀 추가. (화: 나타·미호·비스킷·클로에·리나)
var RecommendedTeams = new Dictionary<string, List<List<int>>>
{
    ["화"] = new() { new() { 103, 118, 201, 202, 255 } },
};

// 요일 키(웹) → SiegeStages 키(EnemyDb). 현재 토요일만 데이터 존재.
var DayToStage = new Dictionary<string, string>
{
    ["월"] = "월요일", ["화"] = "화요일", ["수"] = "수요일", ["목"] = "목요일",
    ["금"] = "금요일", ["토"] = "토요일", ["일"] = "일요일",
};

// 사용 가능한 요일 목록 (데이터가 있는 것만)
app.MapGet("/api/siege/days", () =>
    DayToStage
        .Where(kv => EnemyDb.SiegeStages.ContainsKey(kv.Value))
        .Select(kv => new { day = kv.Key, stage = EnemyDb.SiegeStages[kv.Value].Name }));

// 펫 목록 (검증용 펫 선택)
app.MapGet("/api/siege/pets", () =>
    PetDb.Pets.Select(p => new { id = p.Id, name = p.Name, rarity = p.Rarity }));

// 탐색 실행: 선택 영웅 풀 → 전원 2/4/6초월 3루트.
//   캐시 HIT면 즉시 반환(done). MISS면 백그라운드 잡 시작(running) → 프론트가 /job 폴링.
//   잠재 0/0/0 고정. 전용장비 전체포함(조율탐색)/전체제외 체크박스(includeExclusive).
app.MapPost("/api/siege/optimize", (OptimizeRequest req) =>
{
    if (req?.HeroIds == null || req.HeroIds.Count < 5)
        return Results.BadRequest(new { error = "영웅을 5명 이상 선택하세요." });
    if (string.IsNullOrEmpty(req.Day) || !DayToStage.TryGetValue(req.Day, out var stageKey)
        || !EnemyDb.SiegeStages.TryGetValue(stageKey, out var stage))
        return Results.BadRequest(new { error = $"'{req.Day}' 요일 공성전 데이터가 없습니다." });
    foreach (var id in req.HeroIds)
        if (CharacterDb.Characters.All(c => c.Id != id))
            return Results.BadRequest(new { error = $"영웅 id {id}를 찾을 수 없습니다." });

    bool incl = req.IncludeExclusive ?? true;
    var cached = new Dictionary<string, OptimizeResponse>();
    var missing = new List<int>();
    foreach (int tr in new[] { 2, 4, 6 })
    {
        if (SiegeApi.SiegeCache.TryGet<OptimizeResponse>(SiegeApi.SiegeCache.Key(req.Day, req.HeroIds, tr, incl), out var hit))
        {
            hit.Cached = true;
            cached[tr.ToString()] = hit;
        }
        else missing.Add(tr);
    }

    if (missing.Count == 0)
        return Results.Ok(new { status = "done", day = req.Day, includeExclusive = incl, routes = cached });

    // MISS → 백그라운드 잡 시작(이미 있으면 그대로 진행 상황 반환). 잡 = (요일·영웅·전용옵션) 결정적 id.
    string jobId = JobKey(req.Day, req.HeroIds, incl);
    var job = new JobState { Status = "running" };
    foreach (var kv in cached) job.Routes[kv.Key] = kv.Value;
    if (JobStore.Jobs.TryAdd(jobId, job))
        _ = Task.Run(() => RunJob(jobId, stage, req, incl, missing));
    var cur = JobStore.Jobs[jobId];
    return Results.Ok(new { status = cur.Status, jobId, day = req.Day, includeExclusive = incl, routes = cur.Routes, error = cur.Error });
});

// 잡 폴링: 진행 상황·완료된 루트 반환.
app.MapGet("/api/siege/job", (string id) =>
    JobStore.Jobs.TryGetValue(id, out var j)
        ? Results.Ok(new { status = j.Status, jobId = id, routes = j.Routes, error = j.Error })
        : Results.NotFound(new { error = "작업을 찾을 수 없습니다." }));

// 프리필: 요일별 추천 팀들의 잡을 미리 큐잉(캐시 채우기). 백그라운드 직렬 실행.
app.MapPost("/api/siege/prefill", () =>
{
    int started = 0, alreadyCached = 0;
    foreach (var (day, teams) in RecommendedTeams)
    {
        if (!DayToStage.TryGetValue(day, out var sk) || !EnemyDb.SiegeStages.TryGetValue(sk, out var st)) continue;
        foreach (var ids in teams)
            foreach (bool incl in new[] { true, false })
            {
                var missing = new[] { 2, 4, 6 }
                    .Where(tr => !SiegeApi.SiegeCache.Has(SiegeApi.SiegeCache.Key(day, ids, tr, incl))).ToList();
                if (missing.Count == 0) { alreadyCached++; continue; }
                string jobId = JobKey(day, ids, incl);
                var req = new OptimizeRequest { Day = day, HeroIds = ids, IncludeExclusive = incl };
                if (JobStore.Jobs.TryAdd(jobId, new JobState { Status = "running" }))
                {
                    _ = Task.Run(() => RunJob(jobId, st, req, incl, missing));
                    started++;
                }
            }
    }
    return Results.Ok(new { enqueued = started, alreadyCached });
});

app.Run();

// ===== 잡 (백그라운드 탐색) =====
static string JobKey(string day, List<int> ids, bool incl) =>
    $"{day}|{string.Join(",", ids.OrderBy(i => i))}|x{(incl ? 1 : 0)}";

// 한 잡 = 누락 루트들을 순차 탐색(공유 static 레이스 방지 위해 Gate로 전역 직렬화), 각 루트 완료 시 캐시·잡 갱신.
static async Task RunJob(string jobId, Stage stage, OptimizeRequest req, bool incl, List<int> missing)
{
    await JobStore.Gate.WaitAsync();
    try
    {
        foreach (int tr in missing)
        {
            var dto = RunRoute(stage, req, tr, incl);
            dto.Cached = false;
            SiegeApi.SiegeCache.Set(SiegeApi.SiegeCache.Key(req.Day, req.HeroIds, tr, incl), dto);
            JobStore.Jobs[jobId].Routes[tr.ToString()] = dto;
        }
        JobStore.Jobs[jobId].Status = "done";
    }
    catch (Exception ex)
    {
        JobStore.Jobs[jobId].Status = "error";
        JobStore.Jobs[jobId].Error = ex.Message;
    }
    finally { JobStore.Gate.Release(); }
}

// ===== 한 루트(전원 transcend초월) 탐색 실행 → DTO =====
static OptimizeResponse RunRoute(Stage stage, OptimizeRequest req, int transcend, bool includeExclusive)
{
    var candidates = new List<BattleCharacter>();
    foreach (var id in req.HeroIds)
    {
        var ch = CharacterDb.Characters.First(c => c.Id == id);
        if (!includeExclusive) ch.ExclusiveWeapon = null;   // 전용 제외: 전용무기 미장착
        candidates.Add(new BattleCharacter
        {
            Character = ch,
            TranscendLevel = transcend,
            IsSkillEnhanced = true,
            // 잠재 0/0/0 고정 (BattleCharacter 기본값)
        });
    }

    var config = new SiegeOptimizerConfig
    {
        Candidates = candidates,
        SiegeStage = stage,
        MaxTurns = req.MaxTurns ?? 70,
        PartySize = req.PartySize ?? 5,
        SearchExclusiveWeapon = includeExclusive,   // 전체포함 시 전용조율 탐색
    };

    if (req.Pet != null && !string.IsNullOrEmpty(req.Pet.Name))
    {
        var pet = PetDb.GetByName(req.Pet.Name);
        if (pet != null)
        {
            config.AllyPet = pet;
            config.PetStar = req.Pet.Star is >= 1 and <= 6 ? req.Pet.Star : 6;
            config.PetEnhance = Math.Clamp(req.Pet.Enhance, 0, 3);
            config.PetOptionAtkRate = req.Pet.OptAtkRate;
            config.PetOptionDefRate = req.Pet.OptDefRate;
            config.PetOptionHpRate = req.Pet.OptHpRate;
        }
    }

    var result = new SiegeOptimizer().Optimize(config);
    var dto = ToDto(result);
    dto.Transcend = transcend;
    return dto;
}

// ===== DTO 변환 (Character 객체 그래프 대신 웹용 평면 구조) =====
static OptimizeResponse ToDto(SiegeOptimizerResult r)
{
    var byIndex = (r.BestResult?.CharacterResults ?? new())
        .ToDictionary(c => c.PartyIndex, c => c);

    // 버프 대상 = 딜러(공격/마법/만능형) 중 실제 누적딜 top-2 (비스킷 장비강화·라이언 쿨감 수령자).
    var dealerRoles = new HashSet<string> { "공격형", "마법형", "만능형" };
    var buffTargetIdx = r.BestParty
        .Select((bc, i) => (i, bc, dmg: byIndex.TryGetValue(i, out var cr) ? cr.TotalDamage : 0))
        .Where(x => x.bc.Character.Type != null && dealerRoles.Contains(x.bc.Character.Type))
        .OrderByDescending(x => x.dmg).Take(2).Select(x => x.i).ToHashSet();

    var party = r.BestParty.Select((bc, i) =>
    {
        byIndex.TryGetValue(i, out var cr);
        return new PartyMemberDto
        {
            Id = bc.Character.Id,
            Name = bc.Character.Name,
            Role = bc.Character.Type,
            Transcend = bc.TranscendLevel,
            Position = i + 1,
            TotalDamage = cr?.TotalDamage ?? 0,
            DamageShare = cr?.DamageShare ?? 0,
            IsBackRow = r.BestBackRow?.Contains(bc.Character.Name) ?? false,
            IsBuffTarget = buffTargetIdx.Contains(i),
        };
    }).ToList();

    return new OptimizeResponse
    {
        Score = r.BestScore,
        Formation = r.BestFormation,
        EvaluatedCount = r.EvaluatedCount,
        TotalTurns = r.BestResult?.TotalTurns ?? 0,
        RoundsCleared = r.BestResult?.RoundsCleared ?? 0,
        RoundScore = (r.BestResult?.RoundScore ?? new())
            .OrderBy(kv => kv.Key)
            .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
        GearLog = r.GearLog ?? new(),
        Party = party,
        // 최적 스킬 순서 (빔서치 결과) — 유저 핵심 목적. 스킬턴 순서대로 영웅·스킬명.
        SkillOrder = (r.BestRotationPlan ?? new()).Select((d, i) =>
        {
            if (d.Hold || d.HeroIndex < 0 || d.HeroIndex >= r.BestParty.Count)
                return new SkillStepDto { Step = i + 1, Hero = "(홀드)", Skill = "" };
            var bc = r.BestParty[d.HeroIndex];
            var sk = bc.Character.Skills?.FirstOrDefault(s => s.SkillType == d.Skill);
            return new SkillStepDto { Step = i + 1, Hero = bc.Character.Name, Skill = sk?.Name ?? d.Skill.ToString() };
        }).ToList(),
        TurnLogs = (r.BestResult?.TurnLogs ?? new()).Select(t => new TurnLogDto
        {
            Turn = t.Turn,
            Actor = t.ActorName,
            IsAlly = t.IsAlly,
            ActionType = t.ActionType.ToString(),
            SkillName = t.SkillName,
            Damage = t.DamageDealt,
            Description = t.Description,
        }).ToList(),
    };
}

// ===== 백그라운드 잡 저장소 =====
static class JobStore
{
    // jobId → 상태. 결정적 jobId(요일·영웅·전용옵션)라 같은 팀 재요청 시 동일 잡 공유.
    public static readonly ConcurrentDictionary<string, JobState> Jobs = new();
    // 공유 static(Character.ExclusiveWeapon 등) 레이스 방지 — 탐색을 전역 1개씩 직렬화.
    public static readonly SemaphoreSlim Gate = new(1, 1);
}

class JobState
{
    public string Status { get; set; } = "running";   // running / done / error
    public ConcurrentDictionary<string, OptimizeResponse> Routes { get; set; } = new();
    public string Error { get; set; }
}

// ===== 요청/응답 모델 =====
class PetInput
{
    public string Name { get; set; }
    public int Star { get; set; } = 6;
    public int Enhance { get; set; }       // 0~3 (6성에서만 실제 반영)
    public double OptAtkRate { get; set; } // 펫 옵션 공격력%
    public double OptDefRate { get; set; }
    public double OptHpRate { get; set; }
}

class OptimizeRequest
{
    public string Day { get; set; }
    public List<int> HeroIds { get; set; }             // 선택 영웅 id (5명 이상). 잠재 0/0/0·강화·초월은 서버 고정/루트.
    public bool? IncludeExclusive { get; set; }         // 전용장비 전체포함(조율탐색)/전체제외. 기본 true.
    public PetInput Pet { get; set; }
    public int? MaxTurns { get; set; }
    public int? PartySize { get; set; }
}

class OptimizeResponse
{
    public int Transcend { get; set; }            // 이 루트의 전원 초월 단계 (2/4/6)
    public bool Cached { get; set; }              // 캐시에서 즉시 반환됐는지
    public double Score { get; set; }
    public string Formation { get; set; }
    public int EvaluatedCount { get; set; }
    public int TotalTurns { get; set; }
    public int RoundsCleared { get; set; }
    public Dictionary<string, double> RoundScore { get; set; }
    public List<string> GearLog { get; set; }   // 영웅별 선택 장비 메인옵/부옵 값
    public List<PartyMemberDto> Party { get; set; }
    public List<SkillStepDto> SkillOrder { get; set; }   // 최적 스킬 순서 (스킬턴 순)
    public List<TurnLogDto> TurnLogs { get; set; }
}

class SkillStepDto
{
    public int Step { get; set; }
    public string Hero { get; set; }
    public string Skill { get; set; }
}

class PartyMemberDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Role { get; set; }          // 공격형/마법형/만능형/방어형/지원형
    public int Transcend { get; set; }
    public int Position { get; set; }
    public double TotalDamage { get; set; }
    public double DamageShare { get; set; }
    public bool IsBackRow { get; set; }        // 진형 후열 배치 여부
    public bool IsBuffTarget { get; set; }     // 비스킷 버프·라이언 쿨감 수령(딜 top-2 딜러)
}

class TurnLogDto
{
    public int Turn { get; set; }
    public string Actor { get; set; }
    public bool IsAlly { get; set; }
    public string ActionType { get; set; }
    public string SkillName { get; set; }
    public double Damage { get; set; }
    public string Description { get; set; }
}
