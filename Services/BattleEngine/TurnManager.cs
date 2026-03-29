using System.Collections.Generic;
using System.Linq;
using GameDamageCalculator.Models;

namespace GameDamageCalculator.Services.BattleEngine
{
    /// <summary>
    /// 턴 순서 및 행동 큐 관리
    ///
    /// 턴 구조:
    /// 배틀 시작(0턴) → 선공 측 스킬(0턴)
    /// → 후공 측 속공1순위 기본공격(1턴) → 후공 측 속공2순위 기본공격(2턴) → 후공 측 스킬(2턴)
    /// → 선공 측 속공1순위 기본공격(3턴) → 선공 측 속공2순위 기본공격(4턴) → 선공 측 스킬(4턴)
    /// → 반복...
    /// </summary>
    public class TurnManager
    {
        private readonly BattleConfig _config;
        private readonly bool _allyFirst; // 아군이 선공인지

        // 속공 순서대로 정렬된 아군 인덱스
        private readonly List<int> _allySpdOrder;

        public TurnManager(BattleConfig config, List<CharacterBattleState> allyStates)
        {
            _config = config;

            // 선공 결정: 아군 총 속공 vs 보스 속공
            double allyTotalSpd = allyStates.Sum(s => s.FinalSpd);
            double bossSpd = config.TargetEnemy?.Stats?.Spd ?? 0;
            _allyFirst = allyTotalSpd >= bossSpd;

            // 아군 속공 순서 정렬 (높은 순)
            _allySpdOrder = allyStates
                .OrderByDescending(s => s.FinalSpd)
                .Select(s => s.PartyIndex)
                .ToList();
        }

        /// <summary>
        /// 선공 측이 아군인지
        /// </summary>
        public bool IsAllyFirst => _allyFirst;

        /// <summary>
        /// 지정된 최대 턴까지의 전체 행동 순서를 생성
        /// </summary>
        public List<BattleAction> GenerateActionQueue(int maxTurns)
        {
            var actions = new List<BattleAction>();
            int currentTurn = 0;

            // 0턴: 선공 측 스킬
            if (_allyFirst)
            {
                actions.Add(new BattleAction
                {
                    Turn = 0,
                    IsAlly = true,
                    IsSkill = true,
                    CharacterIndex = -1 // 스킬 로테이션에서 결정
                });
            }
            else
            {
                actions.Add(new BattleAction
                {
                    Turn = 0,
                    IsAlly = false,
                    IsSkill = true,
                    CharacterIndex = -1
                });
            }

            // 이후 턴 진행: 후공 → 선공 → 후공 → 선공 반복
            // 각 라운드: 기본공격 2회 (속공 순서대로 순환) + 스킬 1회
            bool isFirstSideTurn = false; // 다음은 후공 측 차례
            int allyNormalIdx = 0;        // 아군 기본공격: 속공순 캐릭터 순환 인덱스

            while (currentTurn < maxTurns)
            {
                bool isAllyTurn = isFirstSideTurn ? _allyFirst : !_allyFirst;

                // 기본공격 2회
                for (int i = 0; i < 2 && currentTurn < maxTurns; i++)
                {
                    currentTurn++;

                    if (isAllyTurn)
                    {
                        int charIdx = _allySpdOrder[allyNormalIdx % _allySpdOrder.Count];
                        allyNormalIdx++;
                        actions.Add(new BattleAction
                        {
                            Turn = currentTurn,
                            IsAlly = true,
                            IsSkill = false,
                            CharacterIndex = charIdx
                        });
                    }
                    else
                    {
                        actions.Add(new BattleAction
                        {
                            Turn = currentTurn,
                            IsAlly = false,
                            IsSkill = false,
                            CharacterIndex = -1 // 보스 기본공격
                        });
                    }
                }

                // 기본공격 2회 후 스킬 1회 (같은 턴, 턴 소모 없음)
                actions.Add(new BattleAction
                {
                    Turn = currentTurn,
                    IsAlly = isAllyTurn,
                    IsSkill = true,
                    CharacterIndex = -1 // 스킬 로테이션에서 결정
                });

                isFirstSideTurn = !isFirstSideTurn;
            }

            return actions;
        }
    }

    /// <summary>
    /// 배틀 내 단일 행동
    /// </summary>
    public class BattleAction
    {
        public int Turn { get; set; }
        public bool IsAlly { get; set; }
        public bool IsSkill { get; set; }
        public int CharacterIndex { get; set; } // -1이면 로테이션에서 결정 또는 보스
    }
}
