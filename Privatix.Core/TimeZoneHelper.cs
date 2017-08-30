using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;


namespace Privatix.Core
{
    static class TimeZoneHelper
    {
        public const int ERROR_ACCESS_DENIED = 0x005;
        public const int CORSEC_E_MISSING_STRONGNAME = -2146233317;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool SetTimeZoneInformation([In] ref TimeZoneInformation lpTimeZoneInformation);      

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool SetDynamicTimeZoneInformation([In] ref DynamicTimeZoneInformation lpTimeZoneInformation);


        public static bool SetTimeZone(string timeZoneId)
        {
            try
            {
                Logger.Log.Info("timeZoneId = " + timeZoneId);

                var regTimeZones = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Time Zones");
                var subKeyName = regTimeZones.GetSubKeyNames().Where(s => s == timeZoneId).First();
                var subKey = regTimeZones.OpenSubKey(subKeyName);
                string daylightName = (string)subKey.GetValue("Dlt");
                string standardName = (string)subKey.GetValue("Std");
                byte[] tzi = (byte[])subKey.GetValue("TZI");
                subKey.Close();
                regTimeZones.Close();

                var regTzi = new RegistryTimeZoneInformation(tzi);
               

                bool didSet;
                if (Environment.OSVersion.Version.Major < 6)
                {
                    var tz = new TimeZoneInformation();
                    tz.Bias = regTzi.Bias;
                    tz.DaylightBias = regTzi.DaylightBias;
                    tz.StandardBias = regTzi.StandardBias;
                    tz.DaylightDate = regTzi.DaylightDate;
                    tz.StandardDate = regTzi.StandardDate;
                    tz.DaylightName = daylightName;
                    tz.StandardName = standardName;

                    didSet = TimeZoneHelper.SetTimeZoneInformation(ref tz);
                }
                else
                {
                    var tz = new DynamicTimeZoneInformation();
                    tz.Bias = regTzi.Bias;
                    tz.DaylightBias = regTzi.DaylightBias;
                    tz.StandardBias = regTzi.StandardBias;
                    tz.DaylightDate = regTzi.DaylightDate;
                    tz.StandardDate = regTzi.StandardDate;
                    tz.DaylightName = daylightName;
                    tz.StandardName = standardName;
                    tz.TimeZoneKeyName = subKeyName;
                    tz.DynamicDaylightTimeDisabled = false;

                    didSet = TimeZoneHelper.SetDynamicTimeZoneInformation(ref tz);
                }
                int lastError = Marshal.GetLastWin32Error();

                if (!didSet)
                {
                    if (lastError == TimeZoneHelper.ERROR_ACCESS_DENIED)
                    {
                        Logger.Log.Error("Error: Access denied... Try running application as administrator.");
                    }
                    else if (lastError == TimeZoneHelper.CORSEC_E_MISSING_STRONGNAME)
                    {
                        Logger.Log.Error("Error: Application is not signed ... Right click the project > Signing > Check 'Sign the assembly'.");
                    }
                    else
                    {
                        Logger.Log.Error("Win32Error: " + lastError + "\nHRESULT: " + Marshal.GetHRForLastWin32Error());
                    }
                }

                return didSet;
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex.Message, ex);

                return false;
            }
        }
    }


    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct SystemTime
    {
        [MarshalAs(UnmanagedType.U2)]
        public short Year;
        [MarshalAs(UnmanagedType.U2)]
        public short Month;
        [MarshalAs(UnmanagedType.U2)]
        public short DayOfWeek;
        [MarshalAs(UnmanagedType.U2)]
        public short Day;
        [MarshalAs(UnmanagedType.U2)]
        public short Hour;
        [MarshalAs(UnmanagedType.U2)]
        public short Minute;
        [MarshalAs(UnmanagedType.U2)]
        public short Second;
        [MarshalAs(UnmanagedType.U2)]
        public short Milliseconds;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct TimeZoneInformation
    {
        [MarshalAs(UnmanagedType.I4)]
        public int Bias;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        public string StandardName;
        public SystemTime StandardDate;
        [MarshalAs(UnmanagedType.I4)]
        public int StandardBias;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        public string DaylightName;
        public SystemTime DaylightDate;
        [MarshalAs(UnmanagedType.I4)]
        public int DaylightBias;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DynamicTimeZoneInformation
    {
        public int Bias;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string StandardName;
        public SystemTime StandardDate;
        public int StandardBias;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DaylightName;
        public SystemTime DaylightDate;
        public int DaylightBias;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string TimeZoneKeyName;
        [MarshalAs(UnmanagedType.U1)]
        public bool DynamicDaylightTimeDisabled;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RegistryTimeZoneInformation
    {
        [MarshalAs(UnmanagedType.I4)]
        public int Bias;
        [MarshalAs(UnmanagedType.I4)]
        public int StandardBias;
        [MarshalAs(UnmanagedType.I4)]
        public int DaylightBias;
        public SystemTime StandardDate;
        public SystemTime DaylightDate;

        public RegistryTimeZoneInformation(TimeZoneInformation tzi)
        {
            this.Bias = tzi.Bias;
            this.StandardDate = tzi.StandardDate;
            this.StandardBias = tzi.StandardBias;
            this.DaylightDate = tzi.DaylightDate;
            this.DaylightBias = tzi.DaylightBias;
        }

        public RegistryTimeZoneInformation(byte[] bytes)
        {
            if ((bytes == null) || (bytes.Length != 0x2c))
            {
                throw new ArgumentException("Argument_InvalidREG_TZI_FORMAT");
            }
            this.Bias = BitConverter.ToInt32(bytes, 0);
            this.StandardBias = BitConverter.ToInt32(bytes, 4);
            this.DaylightBias = BitConverter.ToInt32(bytes, 8);
            this.StandardDate.Year = BitConverter.ToInt16(bytes, 12);
            this.StandardDate.Month = BitConverter.ToInt16(bytes, 14);
            this.StandardDate.DayOfWeek = BitConverter.ToInt16(bytes, 0x10);
            this.StandardDate.Day = BitConverter.ToInt16(bytes, 0x12);
            this.StandardDate.Hour = BitConverter.ToInt16(bytes, 20);
            this.StandardDate.Minute = BitConverter.ToInt16(bytes, 0x16);
            this.StandardDate.Second = BitConverter.ToInt16(bytes, 0x18);
            this.StandardDate.Milliseconds = BitConverter.ToInt16(bytes, 0x1a);
            this.DaylightDate.Year = BitConverter.ToInt16(bytes, 0x1c);
            this.DaylightDate.Month = BitConverter.ToInt16(bytes, 30);
            this.DaylightDate.DayOfWeek = BitConverter.ToInt16(bytes, 0x20);
            this.DaylightDate.Day = BitConverter.ToInt16(bytes, 0x22);
            this.DaylightDate.Hour = BitConverter.ToInt16(bytes, 0x24);
            this.DaylightDate.Minute = BitConverter.ToInt16(bytes, 0x26);
            this.DaylightDate.Second = BitConverter.ToInt16(bytes, 40);
            this.DaylightDate.Milliseconds = BitConverter.ToInt16(bytes, 0x2a);
        }
    }
}
