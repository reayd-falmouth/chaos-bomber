using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using HybridGame.MasterBlaster.Scripts.Player;
using UnityEditor;
using UnityEngine;

public static class PlayerSpriteSheetBuilder
{
    private const string DebugLogPath = "debug-9c4db8.log";
    private const string SessionId = "9c4db8";

    // Hypotheses:
    // H1: Sprites load in correct numeric order by name suffix.
    // H2: If names are not numeric, rect-sort (top->bottom, left->right) yields correct order.
    // H3: Source texture path is wrong / sprites not sliced (LoadAll returns only 1 sprite).

    [MenuItem("HybridGame/MasterBlaster/Rebuild PlayerSpriteSheet (from player.png)")]
    public static void RebuildFromDefaultTexture()
    {
        const string sheetAssetPath = "Assets/Art/MasterBlaster/Sprites/PlayerSpriteSheet.asset";
        const string texturePath = "Assets/Art/MasterBlaster/Textures/player.png";

        var sheet = AssetDatabase.LoadAssetAtPath<PlayerSpriteSheet>(sheetAssetPath);
        if (sheet == null)
        {
            Log("run1", "H3", "PlayerSpriteSheetBuilder.cs:RebuildFromDefaultTexture", "sheet_missing",
                $"{{\"sheetAssetPath\":\"{Escape(sheetAssetPath)}\"}}");
            Debug.LogError($"[PlayerSpriteSheetBuilder] Missing asset at `{sheetAssetPath}`");
            return;
        }

        var sprites = LoadSpritesFromTexture(texturePath, out string ordering, out string reason);
        Log("run1", sprites.Count > 1 ? "H1" : "H3", "PlayerSpriteSheetBuilder.cs:RebuildFromDefaultTexture", "sprites_loaded",
            $"{{\"texturePath\":\"{Escape(texturePath)}\",\"count\":{sprites.Count},\"ordering\":\"{Escape(ordering)}\",\"reason\":\"{Escape(reason)}\"}}");

        if (sprites.Count == 0)
        {
            Debug.LogError($"[PlayerSpriteSheetBuilder] No sprites found in `{texturePath}`. Is it sliced as Multiple?");
            return;
        }

        sheet.orderedSprites = sprites.ToArray();
        if (sheet.spritesPerPlayer <= 0) sheet.spritesPerPlayer = 30;

        EditorUtility.SetDirty(sheet);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Log("run1", "H1", "PlayerSpriteSheetBuilder.cs:RebuildFromDefaultTexture", "sheet_saved",
            $"{{\"sheetAssetPath\":\"{Escape(sheetAssetPath)}\",\"spritesPerPlayer\":{sheet.spritesPerPlayer},\"orderedCount\":{sheet.orderedSprites.Length}}}");

        Debug.Log($"[PlayerSpriteSheetBuilder] Updated `{sheetAssetPath}` with {sprites.Count} sprites ({ordering}).");
    }

    private static List<Sprite> LoadSpritesFromTexture(string texturePath, out string ordering, out string reason)
    {
        ordering = "unknown";
        reason = "";

        var objs = AssetDatabase.LoadAllAssetRepresentationsAtPath(texturePath);
        var sprites = new List<Sprite>(objs.Length);
        foreach (var o in objs)
        {
            if (o is Sprite s) sprites.Add(s);
        }

        // H3 evidence: only 1 sprite => probably not sliced.
        if (sprites.Count <= 1)
        {
            ordering = "as-loaded";
            reason = "sprites_count<=1";
            return sprites;
        }

        // Try numeric suffix order first (H1)
        var numeric = new List<(int n, Sprite s)>(sprites.Count);
        bool allNumeric = true;
        for (int i = 0; i < sprites.Count; i++)
        {
            if (!TryParseTrailingInt(sprites[i].name, out int n))
            {
                allNumeric = false;
                break;
            }
            numeric.Add((n, sprites[i]));
        }

        if (allNumeric)
        {
            numeric.Sort((a, b) => a.n.CompareTo(b.n));
            ordering = "name_numeric_suffix";
            reason = "all_sprites_numeric_suffix";
            var outList = new List<Sprite>(numeric.Count);
            for (int i = 0; i < numeric.Count; i++) outList.Add(numeric[i].s);
            return outList;
        }

        // Fallback: rect sort (H2): top->bottom (y desc), left->right (x asc)
        sprites.Sort((a, b) =>
        {
            var ra = a.rect;
            var rb = b.rect;
            int y = -ra.y.CompareTo(rb.y);
            if (y != 0) return y;
            return ra.x.CompareTo(rb.x);
        });

        ordering = "rect_row_major";
        reason = "fallback_rect_sort";
        return sprites;
    }

    private static bool TryParseTrailingInt(string name, out int n)
    {
        n = 0;
        if (string.IsNullOrEmpty(name)) return false;
        int i = name.Length - 1;
        while (i >= 0 && char.IsDigit(name[i])) i--;
        int start = i + 1;
        if (start >= name.Length) return false;
        return int.TryParse(name.Substring(start), NumberStyles.Integer, CultureInfo.InvariantCulture, out n);
    }

    private static void Log(string runId, string hypothesisId, string location, string message, string dataJson)
    {
        try
        {
            var sb = new StringBuilder(256);
            sb.Append('{');
            sb.Append("\"sessionId\":\"").Append(SessionId).Append("\",");
            sb.Append("\"timestamp\":").Append(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).Append(',');
            sb.Append("\"runId\":\"").Append(runId).Append("\",");
            sb.Append("\"hypothesisId\":\"").Append(hypothesisId).Append("\",");
            sb.Append("\"location\":\"").Append(location).Append("\",");
            sb.Append("\"message\":\"").Append(message).Append("\",");
            sb.Append("\"data\":").Append(string.IsNullOrEmpty(dataJson) ? "{}" : dataJson);
            sb.Append('}');
            sb.Append('\n');
            File.AppendAllText(Path.Combine(Directory.GetCurrentDirectory(), DebugLogPath), sb.ToString());
        }
        catch
        {
            // ignore
        }
    }

    private static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}

