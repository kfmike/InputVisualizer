using System;
using System.ComponentModel;
using System.Linq;

namespace InputVisualizer
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum en)
        {
            var mi = en.GetType().GetMember(en.ToString());
            if ((mi != null && mi.Length > 0))
            {
                var attrs = mi[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attrs?.Length > 0)
                {
                    return ((DescriptionAttribute)attrs.ElementAt(0)).Description;
                }
            }
            return en.ToString();
        }
    }
}
