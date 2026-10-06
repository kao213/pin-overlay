using System.Text.Json;
using System.Text.RegularExpressions;

namespace PinOverlay.Core;

/// <summary>settings.json の読み書き。</summary>
public static partial class SettingsStore
{
    [GeneratedRegex(@"\A#[0-9A-Fa-f]{8}\z")]
    private static partial Regex OutlineColorPattern();

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
    };

    /// <summary>
    /// 設定を読み込む。ファイルがない場合は既定値を返す。
    /// 壊れている場合は settings.json.broken に退避してから既定値を返す。
    /// </summary>
    public static AppSettings Load(string path)
    {
        if (!File.Exists(path))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, Options) ?? new AppSettings();
            settings.TabOrder ??= [];
            settings.Tabs ??= [];
            settings.Text ??= new TextTabSettings();
            // 手で書き換えられた settings.json でも起動時に落ちないよう、null と範囲外の値を正規化する
            foreach (var key in settings.Tabs.Where(pair => pair.Value is null).Select(pair => pair.Key).ToList())
            {
                settings.Tabs.Remove(key);
            }
            settings.TabOrder.RemoveAll(name => name is null);
            foreach (var tab in settings.Tabs.Values)
            {
                tab.CheckedFiles ??= [];
                tab.CheckedFiles.RemoveAll(name => name is null);
            }
            // OutlineColor は文字の縁取りに使う色。#AARRGGBB 以外（ContextColor 指定など）は UNC パスの読み込みに使われうる
            if (settings.OutlineColor is null || !OutlineColorPattern().IsMatch(settings.OutlineColor))
            {
                settings.OutlineColor = new AppSettings().OutlineColor;
            }
            settings.Opacity = double.IsNaN(settings.Opacity) ? 1.0 : Math.Clamp(settings.Opacity, 0.1, 1.0);
            return settings;
        }
        catch (JsonException)
        {
            File.Copy(path, path + ".broken", overwrite: true);
            return new AppSettings();
        }
    }

    /// <summary>設定を保存する。途中で落ちても壊れないよう、一時ファイルに書いてから置き換える。</summary>
    public static void Save(string path, AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, Options);
        var temp = path + ".tmp";
        File.WriteAllText(temp, json);
        File.Move(temp, path, overwrite: true);
    }
}
