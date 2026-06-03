using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SiegeApi
{
    /// <summary>
    /// 공성전 탐색 결과 파일 캐시.
    /// 키 = (요일 + 정렬된 영웅id집합 + 초월루트 + 전용옵션). 값 = 탐색 응답(OptimizeResponse) JSON.
    /// 재시작 후에도 유지 — 요일별 기본 시뮬값을 미리 채워두고, 새 영웅풀만 실탐색한다.
    /// </summary>
    public static class SiegeCache
    {
        private static readonly object _lock = new();
        private static readonly string _path =
            Path.Combine(AppContext.BaseDirectory, "siege_cache.json");
        private static Dictionary<string, JsonElement> _store = Load();

        private static Dictionary<string, JsonElement> Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    var json = File.ReadAllText(_path);
                    var d = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    if (d != null) return d;
                }
            }
            catch { /* 손상 캐시는 무시하고 새로 시작 */ }
            return new Dictionary<string, JsonElement>();
        }

        /// <summary>키 생성: 요일|정렬영웅id|t{초월}|x{전용포함 1/0}.</summary>
        public static string Key(string day, IEnumerable<int> heroIds, int transcend, bool includeExclusive)
        {
            var ids = string.Join(",", heroIds.OrderBy(i => i));
            return $"{day}|{ids}|t{transcend}|x{(includeExclusive ? 1 : 0)}";
        }

        public static bool TryGet<T>(string key, out T value)
        {
            lock (_lock)
            {
                if (_store.TryGetValue(key, out var el))
                {
                    value = el.Deserialize<T>();
                    return true;
                }
            }
            value = default;
            return false;
        }

        public static void Set<T>(string key, T value)
        {
            lock (_lock)
            {
                _store[key] = JsonSerializer.SerializeToElement(value);
                try { File.WriteAllText(_path, JsonSerializer.Serialize(_store)); }
                catch { /* 디스크 실패는 무시(인메모리는 유지) */ }
            }
        }

        public static bool Has(string key)
        {
            lock (_lock) return _store.ContainsKey(key);
        }
    }
}
