using System.Collections.Generic;

namespace GameDamageCalculator.Database
{
    /// <summary>
    /// 서브옵션 데이터
    /// </summary>
    public static class SubOptionDb
    {
        // 서브옵션 목록 (드롭다운용)
        public static List<string> AllStatNames { get; } = new()
        {
            "",
            "공격력%", "공격력", "방어력%", "방어력", "생명력%", "생명력",
            "치명타확률%", "치명타피해%", "약점공격확률%", "막기확률%",
            "효과적중%", "효과저항%", "받피감%", "속공"
        };

        // 티어당 값
        public static Dictionary<string, int> TierValues { get; } = new()
        {
            { "공격력%", 5 },
            { "공격력", 50 },
            { "치명타확률%", 4 },
            { "치명타피해%", 6 },
            { "약점공격확률%", 5 },
            { "속공", 4 },
            { "막기확률%", 4 },
            { "효과적중%", 5 },
            { "효과저항%", 5 },
            { "방어력%", 5 },
            { "방어력", 30 },
            { "생명력%", 5 },
            { "생명력", 180 },
        };
    }
}
