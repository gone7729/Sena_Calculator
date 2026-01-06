namespace GameDamageCalculator.Models
{
    /// <summary>
    /// 게임 모드
    /// </summary>
    public enum GameMode
    {
        /// <summary>PVE - 보스전 (보스 피해% 적용)</summary>
        PVE,
        
        /// <summary>PVP - 대인전 (보스 피해% 미적용)</summary>
        PVP
    }
}
