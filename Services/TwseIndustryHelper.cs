namespace StockAnalysis.Services;

/// <summary>
/// TWSE 產業別代碼 → 中文名稱對照。
/// t187ap03_L 部分股票回傳數字代碼，此處統一解析為中文。
/// </summary>
public static class TwseIndustryHelper
{
    private static readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase)
    {
        // 傳統產業
        ["01"] = "水泥工業",    ["1"]  = "水泥工業",
        ["02"] = "食品工業",    ["2"]  = "食品工業",
        ["03"] = "塑膠工業",    ["3"]  = "塑膠工業",
        ["04"] = "紡織纖維",    ["4"]  = "紡織纖維",
        ["05"] = "電機機械",    ["5"]  = "電機機械",
        ["06"] = "電器電纜",    ["6"]  = "電器電纜",
        ["07"] = "化學工業",    ["7"]  = "化學工業",
        ["08"] = "玻璃陶瓷",    ["8"]  = "玻璃陶瓷",
        ["09"] = "造紙工業",    ["9"]  = "造紙工業",
        ["10"] = "鋼鐵工業",
        ["11"] = "橡膠工業",
        ["12"] = "汽車工業",
        ["13"] = "建材營造",
        ["14"] = "航運業",
        ["15"] = "觀光餐旅",
        ["16"] = "金融保險",
        ["17"] = "貿易百貨",
        ["18"] = "綜合",
        ["19"] = "電子工業",
        ["20"] = "其他",
        // 科技 / 新興
        ["21"] = "化學生技醫療",
        ["22"] = "生技醫療業",
        ["23"] = "油電燃氣業",
        ["24"] = "半導體業",
        ["25"] = "電腦及週邊設備業",
        ["26"] = "光電業",
        ["27"] = "通信網路業",
        ["28"] = "電子零組件業",
        ["29"] = "電子通路業",
        ["30"] = "資訊服務業",
        ["31"] = "其他電子業",
        ["32"] = "文化創意業",
        ["33"] = "農業科技業",
        ["34"] = "電子商務",
        // 其他分類
        ["80"] = "管理股票",
        ["90"] = "存託憑證",
        ["00"] = "其他",
        ["0"]  = "其他",
    };

    /// <summary>
    /// 若輸入為數字代碼則轉換為中文，否則原字串回傳。
    /// </summary>
    public static string Resolve(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "其他";
        var key = raw.Trim();
        return _map.TryGetValue(key, out var name) ? name : key;
    }

    /// <summary>判斷是否為需要轉換的代碼（純數字）</summary>
    public static bool IsCode(string? raw)
        => !string.IsNullOrWhiteSpace(raw) && raw.Trim().All(char.IsDigit);
}
