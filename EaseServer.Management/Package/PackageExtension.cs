using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EaseServer.Management.Package
{
    public static class PackageExtension
    {
        /// <summary>
        /// 当前时间与协调世界时(utc)1970年1月1日午夜之间的时间差（以毫秒为单位测量）
        /// </summary>
        /// <param name="dateTimeUtc"></param>
        public static string GetTimeMilliseconds(this DateTime dateTimeUtc)
        {
            return (dateTimeUtc - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds.ToString("0");
        }

        /// <summary>
        /// 获取时间差微秒数原始时间值
        /// </summary>
        /// <param name="timeMillisecondsString"></param>
        /// <returns></returns>
        public static DateTime GetDatetimeFromTimeMilliseconds(this string timeMillisecondsString)
        {
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(Convert.ToDouble(timeMillisecondsString));
            return new DateTime(1970, 1, 1, 0, 0, 0).Add(timeSpan);
        }

        public static uint DateTimeToDosTime(this DateTime _dt)
        {
            return (uint)(
                (_dt.Second / 2) | (_dt.Minute << 5) | (_dt.Hour << 11) |
                (_dt.Day << 16) | (_dt.Month << 21) | ((_dt.Year - 1980) << 25));
        }

        public static DateTime DosTimeToDateTime(this uint _dt)
        {
            return new DateTime(
                (int)(_dt >> 25) + 1980,
                (int)(_dt >> 21) & 15,
                (int)(_dt >> 16) & 31,

                (int)(_dt >> 11) & 31,
                (int)(_dt >> 5) & 63,
                (int)(_dt & 31) * 2);
        }

    }
}
