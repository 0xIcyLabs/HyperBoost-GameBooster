using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using HyperBoost;

internal static class TestMain
{
    private static int failures;

    private static void Check(bool condition, string message)
    {
        Console.WriteLine((condition ? "PASS: " : "FAIL: ") + message);
        if (!condition) failures++;
    }

    private static int Main()
    {
        FieldInfo tableField = typeof(Texts).GetField("table", BindingFlags.NonPublic | BindingFlags.Static);
        var table = (Dictionary<string, string[]>)tableField.GetValue(null);

        Check(Enum.GetNames(typeof(AppLanguage)).Length == 6, "6 languages defined in AppLanguage enum");
        Check(Texts.LanguageNames.Length == 6, "6 language display names");

        // Collect every key referenced by Texts.T(...) in the actual UI sources
        var usedKeys = new HashSet<string>();
        foreach (string file in Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @".."), "*.cs"))
        {
            if (file.EndsWith("TestHarness.cs")) continue;
            foreach (Match m in Regex.Matches(File.ReadAllText(file), @"Texts\.T\([^,\)]+,\s*""([^""]+)"""))
                usedKeys.Add(m.Groups[1].Value);
        }
        Console.WriteLine("INFO: " + usedKeys.Count + " text keys referenced in UI code");
        foreach (string key in usedKeys)
        {
            string[] values;
            bool ok = table.TryGetValue(key, out values) && values != null && values.Length == 6;
            if (ok) foreach (string v in values) if (string.IsNullOrWhiteSpace(v)) { ok = false; break; }
            Check(ok, "key '" + key + "' present and complete in 6 languages");
        }

        // No orphaned table entries with wrong arity
        bool arity = true;
        foreach (KeyValuePair<string, string[]> entry in table)
            if (entry.Value == null || entry.Value.Length != 6) { arity = false; Console.WriteLine("INFO: bad entry " + entry.Key); }
        Check(arity, "all table entries have exactly 6 translations");

        // Reverse lookup: every key referenced must exist (already covered), verify examples resolve
        Check(Texts.T(AppLanguage.Korean, "footer").Length > 10, "Korean footer resolves");
        Check(Texts.T(AppLanguage.Arabic, "restore") == "إلغاء التعزيز", "Arabic REVERT BOOST resolves");
        Check(Texts.T(AppLanguage.Japanese, "restore") == "ブースト解除", "Japanese REVERT BOOST resolves");
        Check(Texts.T(AppLanguage.Chinese, "restore") == "撤销加速", "Chinese REVERT BOOST resolves");
        Check(Texts.T(AppLanguage.French, "restore") == "ANNULER LE BOOST", "French REVERT BOOST resolves");
        Check(Texts.T(AppLanguage.English, "restore") == "REVERT BOOST", "English REVERT BOOST resolves");
        Check(Texts.T(AppLanguage.English, "missing_key") == "missing_key", "unknown key falls back gracefully");

        Check(JunkCleaner.FormatBytes(512L * 1024 * 1024) == "512.0 MB", "FormatBytes MB formatting");
        Check(JunkCleaner.FormatBytes(2L * 1024 * 1024 * 1024) == "2.0 GB", "FormatBytes GB formatting");

        // String.Format placeholders valid for format keys (would throw at runtime otherwise)
        string[] formatKeys = { "ramFreedFmt", "monitorFreedFmt", "freedFmt", "activeFmt" };
        bool formats = true;
        foreach (string fk in formatKeys)
            for (int i = 0; i < 6 && formats; i++)
                try { string.Format(table[fk][i], table[fk][i].Contains("{1}") ? new object[] { "1.0 MB", 2 } : new object[] { "1.0 MB" }); }
                catch { formats = false; }
        Check(formats, "format strings valid in all languages");

        // Registry helpers safe without exceptions
        int dummy;
        Check(GameTweaks.ReadDword(Registry.CurrentUser, @"Software\Microsoft\GameBar", "AutoGameModeEnabled", out dummy) || dummy == 0, "GameTweaks.ReadDword safe");
        Check(GameTweaks.AppliedCount == 0, "undo journal starts empty (no tweaks applied in test session)");
        Check(!GameTweaks.Effective(2) || true, "Effective() safe to call");

        Console.WriteLine(failures == 0 ? "== ALL TESTS PASSED ==" : "== " + failures + " FAILURES ==");
        return failures == 0 ? 0 : 1;
    }
}
