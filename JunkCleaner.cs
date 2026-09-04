using System;
using System.Collections.Generic;
using System.IO;

namespace HyperBoost
{
    internal struct SizeResult { internal long Bytes; internal int Files; }
    internal struct CleanResult { internal long FreedBytes; internal int DeletedFiles; internal int SkippedFiles; }

    internal static class JunkCleaner
    {
        internal static string[] All()
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return new[]
            {
                Environment.ExpandEnvironmentVariables(@"%SystemRoot%\Temp"),
                Path.Combine(local, "Temp"),
                Environment.ExpandEnvironmentVariables(@"%SystemRoot%\SoftwareDistribution\Download")
            };
        }

        internal static string Name(AppLanguage language, int index)
        {
            switch (index)
            {
                case 0: return Texts.T(language, "tempWin");
                case 1: return Texts.T(language, "tempUser");
                case 2: return Texts.T(language, "wuCache");
                default: return "";
            }
        }

        private static bool IsAllowedRoot(string root)
        {
            foreach (string allowed in All())
                if (string.Equals(Path.GetFullPath(root), Path.GetFullPath(allowed), StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        internal static SizeResult Measure(string root)
        {
            var result = new SizeResult();
            if (!IsAllowedRoot(root) || !Directory.Exists(root)) return result;
            var stack = new Stack<string>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                string dir = stack.Pop();
                List<string> entries;
                try { entries = new List<string>(Directory.EnumerateFileSystemEntries(dir)); }
                catch { continue; }
                foreach (string entry in entries)
                {
                    try
                    {
                        var attributes = File.GetAttributes(entry);
                        if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
                        {
                            if ((attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint) continue;
                            stack.Push(entry);
                        }
                        else
                        {
                            result.Bytes += new FileInfo(entry).Length;
                            result.Files++;
                        }
                    }
                    catch { }
                }
            }
            return result;
        }

        internal static CleanResult Clean(string root)
        {
            var result = new CleanResult();
            if (!IsAllowedRoot(root) || !Directory.Exists(root)) return result;
            List<string> entries;
            try { entries = new List<string>(Directory.EnumerateFileSystemEntries(root)); }
            catch { return result; }
            foreach (string entry in entries)
            {
                try
                {
                    var attributes = File.GetAttributes(entry);
                    if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        long size = 0;
                        try { size = Measure(entry).Bytes; }
                        catch { }
                        Directory.Delete(entry, true);
                        result.FreedBytes += size;
                        result.DeletedFiles++;
                    }
                    else
                    {
                        result.FreedBytes += new FileInfo(entry).Length;
                        File.Delete(entry);
                        result.DeletedFiles++;
                    }
                }
                catch { result.SkippedFiles++; }
            }
            return result;
        }

        internal static string FormatBytes(long bytes)
        {
            double mb = bytes / 1048576.0;
            return mb >= 1024 ? string.Format("{0:0.0} GB", mb / 1024.0) : string.Format("{0:0.0} MB", mb);
        }
    }
}
