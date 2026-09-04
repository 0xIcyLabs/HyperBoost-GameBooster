using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace HyperBoost
{
    internal static class MemoryInfo
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct StatusEx
        {
            internal uint Length; internal uint MemoryLoad; internal ulong TotalPhys; internal ulong AvailPhys;
            internal ulong TotalPageFile; internal ulong AvailPageFile; internal ulong TotalVirtual; internal ulong AvailVirtual; internal ulong AvailExtendedVirtual;
        }
        [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GlobalMemoryStatusEx(ref StatusEx buffer);
        internal static ulong AvailablePhysicalBytes()
        {
            var status = new StatusEx { Length = (uint)Marshal.SizeOf(typeof(StatusEx)) };
            GlobalMemoryStatusEx(ref status);
            return status.AvailPhys;
        }
    }

    internal static class MemoryCleaner
    {
        private const int ProcessSetQuota = 0x0100, ProcessQueryInformation = 0x0400, ProcessQueryLimitedInformation = 0x1000;
        private const uint TokenQuery = 0x0008, TokenAdjustPrivileges = 0x0020, PrivilegeEnabled = 0x00000002;
        [StructLayout(LayoutKind.Sequential)] private struct Luid { internal uint LowPart; internal int HighPart; }
        [StructLayout(LayoutKind.Sequential)] private struct LuidAndAttributes { internal Luid Luid; internal uint Attributes; }
        [StructLayout(LayoutKind.Sequential)] private struct TokenPrivileges { internal uint PrivilegeCount; internal LuidAndAttributes Privileges; }
        [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr OpenProcess(int access, bool inherit, int pid);
        [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool CloseHandle(IntPtr handle);
        [DllImport("psapi.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool EmptyWorkingSet(IntPtr handle);
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool LookupPrivilegeValue(string system, string name, out Luid luid);
        [DllImport("advapi32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool AdjustTokenPrivileges(IntPtr token, bool disableAll, ref TokenPrivileges newState, int length, IntPtr previous, IntPtr returnLength);
        [DllImport("ntdll.dll")] private static extern int NtSetSystemInformation(int infoClass, IntPtr info, int infoLength);

        internal static void PurgeAll()
        {
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    IntPtr handle = OpenProcess(ProcessQueryLimitedInformation | ProcessSetQuota, false, process.Id);
                    if (handle == IntPtr.Zero) handle = OpenProcess(ProcessQueryInformation | ProcessSetQuota, false, process.Id);
                    if (handle == IntPtr.Zero) continue;
                    EmptyWorkingSet(handle);
                    CloseHandle(handle);
                }
                catch { }
                finally { process.Dispose(); }
            }
            PurgeSystemLists();
        }

        private static void PurgeSystemLists()
        {
            EnablePrivilege("SeProfileSingleProcessPrivilege");
            foreach (int command in new[] { 3, 5, 4 })
            foreach (int infoClass in new[] { 88, 80 })
            {
                IntPtr buffer = Marshal.AllocHGlobal(4);
                try { Marshal.WriteInt32(buffer, command); if (NtSetSystemInformation(infoClass, buffer, 4) >= 0) break; }
                finally { Marshal.FreeHGlobal(buffer); }
            }
        }

        private static void EnablePrivilege(string name)
        {
            IntPtr token;
            if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TokenAdjustPrivileges | TokenQuery, out token)) return;
            try
            {
                Luid luid;
                if (LookupPrivilegeValue(null, name, out luid))
                {
                    var privileges = new TokenPrivileges { PrivilegeCount = 1, Privileges = new LuidAndAttributes { Luid = luid, Attributes = PrivilegeEnabled } };
                    AdjustTokenPrivileges(token, false, ref privileges, 0, IntPtr.Zero, IntPtr.Zero);
                }
            }
            finally { CloseHandle(token); }
        }
    }

    internal sealed class RamMonitor : IDisposable
    {
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 4000 };
        private readonly Action<long> freedCallback;
        private readonly ulong thresholdBytes = 2048UL * 1024UL * 1024UL;
        private DateTime lastPurge = DateTime.MinValue;
        private bool busy;
        private volatile bool disposed;

        internal RamMonitor(Action<long> onFreed)
        {
            freedCallback = onFreed;
            timer.Tick += async delegate { await Check(); };
        }
        internal void Start() { timer.Start(); }
        public void Dispose() { disposed = true; timer.Dispose(); }

        private async Task Check()
        {
            if (disposed || busy) return;
            ulong available = MemoryInfo.AvailablePhysicalBytes();
            if (available >= thresholdBytes) return;
            if ((DateTime.Now - lastPurge).TotalSeconds < 15) return;
            busy = true;
            try
            {
                ulong before = available;
                await Task.Run(() => MemoryCleaner.PurgeAll());
                if (disposed) return;
                lastPurge = DateTime.Now;
                ulong after = MemoryInfo.AvailablePhysicalBytes();
                long freedMb = (long)(after / 1024UL / 1024UL) - (long)(before / 1024UL / 1024UL);
                if (freedMb > 0 && freedCallback != null) freedCallback(freedMb);
            }
            finally { busy = false; }
        }
    }
}
