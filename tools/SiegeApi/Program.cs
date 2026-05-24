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

// 요일 키(웹) → SiegeStages 키(EnemyDb). 현재 토요일만 데이터 존재.
var DayToStage = new Dictionary<string, string>
{
    ["토"] = "토요일",
};

// 사용 가능한 요일 목록 (데이터가 있는 것만)
app.MapGet("/api/siege/days", () =>
    DayToStage
        .Where(kv => EnemyDb.SiegeStages.ContainsKey(kv.Value))
        .Select(kv => new { day = kv.Key, stage = EnemyDb.SiegeStages[kv.Value].Name }));

// 펫 목록 (검증용 펫 선택)
app.MapGet("/api/siege/pets", () =>
    PetDb.Pets.Select(p => new { id = p.Id, name = p.Name, rarity = p.Rarity }));

// 탐색 실행: 선택 영웅 풀에서 최고딜 5인 팀 + 진형 탐색
app.MapPost("/api/siege/optimize", (OptimizeRequest req) =>
{
    if (req?.Members == null || req.Members.Count < 5)
        return Results.BadRequest(new { error = "영웅을 5명 이상 선택하세요." });

    if (string.IsNullOrEmpty(req.Day) || !DayToStage.TryGetValue(req.Day, out var stageKey)
        || !EnemyDb.SiegeStages.TryGetValue(stageKey, out var stage))
        return Results.BadRequest(new { error = $"'{req.Day}' 요일 공성전 데이터가 없습니다." });

    // 웹 영웅 id → CharacterDb 매핑 → BattleCharacter
    var candidates = new List<BattleCharacter>();
    foreach (var m in req.Members)
    {
        var ch = CharacterDb.Characters.FirstOrDefault(c => c.Id == m.Id);
        if (ch == null) return Results.BadRequest(new { error = $"영웅 id {m.Id}를 찾을 수 없습니다." });
        candidates.Add(new BattleCharacter
        {
            Character = ch,
            TranscendLevel = Math.Clamp(m.Transcend, 0, 12),
            IsSkillEnhanced = m.SkillEnhanced ?? true,
        });
    }

    var config = new SiegeOptimizerConfig
    {
        Candidates = candidates,
        SiegeStage = stage,
        MaxTurns = req.MaxTurns ?? 70,
        PartySize = req.PartySize ?? 5,
    };

    // 펫 (선택). 검증 시 게임 세팅 그대로 맞추려면 펫·성급·강화·옵션이 필요.
    if (req.Pet != null && !string.IsNullOrEmpty(req.Pet.Name))
    {
        var pet = PetDb.GetByName(req.Pet.Name);
        if (pet == null) return Results.BadRequest(new { error = $"펫 '{req.Pet.Name}'을(를) 찾을 수 없습니다." });
        config.AllyPet = pet;
        config.PetStar = req.Pet.Star is >= 1 and <= 6 ? req.Pet.Star : 6;
        config.PetEnhance = Math.Clamp(req.Pet.Enhance, 0, 3);  // 6성에서만 실제 반영(Pet.GetSkillBuff 가드)
        config.PetOptionAtkRate = req.Pet.OptAtkRate;
        config.PetOptionDefRate = req.Pet.OptDefRate;
        config.PetOptionHpRate = req.Pet.OptHpRate;
    }

    var result = new SiegeOptimizer().Optimize(config);
    return Results.Ok(ToDto(result));
});

app.Run();

// ===== DTO 변환 (Character 객체 그래프 대신 웹용 평면 구조) =====
static OptimizeResponse ToDto(SiegeOptimizerResult r)
{
    var byIndex = (r.BestResult?.CharacterResults ?? new())
        .ToDictionary(c => c.PartyIndex, c => c);

    var party = r.BestParty.Select((bc, i) =>
    {
        byIndex.TryGetValue(i, out var cr);
        return new PartyMemberDto
        {
            Id = bc.Character.Id,
            Name = bc.Character.Name,
            Transcend = bc.TranscendLevel,
            Position = i + 1,
            TotalDamage = cr?.TotalDamage ?? 0,
            DamageShare = cr?.DamageShare ?? 0,
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

// ===== 요청/응답 모델 =====
record MemberInput(int Id, int Transcend, bool? SkillEnhanced);

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
    public List<MemberInput> Members { get; set; }
    public PetInput Pet { get; set; }
    public int? MaxTurns { get; set; }
    public int? PartySize { get; set; }
}

class OptimizeResponse
{
    public double Score { get; set; }
    public string Formation { get; set; }
    public int EvaluatedCount { get; set; }
    public int TotalTurns { get; set; }
    public int RoundsCleared { get; set; }
    public Dictionary<string, double> RoundScore { get; set; }
    public List<string> GearLog { get; set; }   // 영웅별 선택 장비 메인옵/부옵 값
    public List<PartyMemberDto> Party { get; set; }
    public List<TurnLogDto> TurnLogs { get; set; }
}

class PartyMemberDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Transcend { get; set; }
    public int Position { get; set; }
    public double TotalDamage { get; set; }
    public double DamageShare { get; set; }
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
