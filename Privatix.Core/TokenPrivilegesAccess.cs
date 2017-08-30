using System;
using System.Runtime.InteropServices;


namespace Privatix.Core
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct LUID
    {
        internal uint LowPart;
        internal uint HighPart;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct LUID_AND_ATTRIBUTES
    {
        internal LUID Luid;
        internal uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct TOKEN_PRIVILEGE
    {
        internal uint PrivilegeCount;
        internal LUID_AND_ATTRIBUTES Privilege;
    }


    public static class TokenPrivilegesAccess
    {
        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        public static extern int OpenProcessToken(int ProcessHandle, int DesiredAccess,
        ref int tokenhandle);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetCurrentProcess();

        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        public static extern int LookupPrivilegeValue(string lpsystemname, string lpname,
        [MarshalAs(UnmanagedType.Struct)] ref LUID lpLuid);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        public static extern int AdjustTokenPrivileges(int tokenhandle, int disableprivs,
            [MarshalAs(UnmanagedType.Struct)]ref TOKEN_PRIVILEGE Newstate, int bufferlength,
            int PreivousState, int Returnlength);

        public const int TOKEN_ASSIGN_PRIMARY = 0x00000001;
        public const int TOKEN_DUPLICATE = 0x00000002;
        public const int TOKEN_IMPERSONATE = 0x00000004;
        public const int TOKEN_QUERY = 0x00000008;
        public const int TOKEN_QUERY_SOURCE = 0x00000010;
        public const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;
        public const int TOKEN_ADJUST_GROUPS = 0x00000040;
        public const int TOKEN_ADJUST_DEFAULT = 0x00000080;

        public const UInt32 SE_PRIVILEGE_ENABLED_BY_DEFAULT = 0x00000001;
        public const UInt32 SE_PRIVILEGE_ENABLED = 0x00000002;
        public const UInt32 SE_PRIVILEGE_REMOVED = 0x00000004;
        public const UInt32 SE_PRIVILEGE_USED_FOR_ACCESS = 0x80000000;

        public static bool EnablePrivilege(string privilege)
        {
            try
            {
                int token = 0;
                int retVal = 0;

                TOKEN_PRIVILEGE TP = new TOKEN_PRIVILEGE();
                LUID LD = new LUID();

                retVal = OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref token);
                retVal = LookupPrivilegeValue(null, privilege, ref LD);
                TP.PrivilegeCount = 1;

                var luidAndAtt = new LUID_AND_ATTRIBUTES();
                luidAndAtt.Attributes = SE_PRIVILEGE_ENABLED;
                luidAndAtt.Luid = LD;
                TP.Privilege = luidAndAtt;

                retVal = AdjustTokenPrivileges(token, 0, ref TP, 1024, 0, 0);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool DisablePrivilege(string privilege)
        {
            try
            {
                int token = 0;
                int retVal = 0;

                TOKEN_PRIVILEGE TP = new TOKEN_PRIVILEGE();
                LUID LD = new LUID();

                retVal = OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref token);
                retVal = LookupPrivilegeValue(null, privilege, ref LD);
                TP.PrivilegeCount = 1;
                // TP.Attributes should be none (not set) to disable privilege
                var luidAndAtt = new LUID_AND_ATTRIBUTES();
                luidAndAtt.Luid = LD;
                TP.Privilege = luidAndAtt;

                retVal = AdjustTokenPrivileges(token, 0, ref TP, 1024, 0, 0);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
