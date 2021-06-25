using System;
using System.Globalization;

namespace IOTLinkAPI.Helpers
{
    public abstract class MathHelper
    {
        public static bool ToBoolean(object value, bool defaultValue = false)
        {
            try
            {
                return Convert.ToBoolean(value);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public static int ToInteger(object value, int defaultValue = 0)
        {
            try
            {
                return Convert.ToInt32(value);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public static long ToLong(object value, long defaultValue = 0L)
        {
            try
            {
                return Convert.ToInt64(value);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public static double ToDouble(object value, double defaultValue = 0d)
        {
            try
            {
                return Convert.ToDouble(value);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public static float ToFloat(object value, float defaultValue = 0f)
        {
            try
            {
                return float.Parse(value.ToString(), CultureInfo.InvariantCulture.NumberFormat);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }
    }
}
