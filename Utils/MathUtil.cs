using System;
using System.Collections.Generic;
using System.Text;

namespace PaulMapper
{
    public static class MathUtil
    {
        public static int CompareRound(float d1, float d2, float rounding)
        {
            if (EqualsRound(d1, d2, rounding))
                return 0;

            if (d1 > d2)
                return 1;
            else
                return -1;
        }

        public static bool EqualsRound(float d1, float d2, float rounding)
        {
            bool result = (Math.Abs(d2 - d1) < rounding);
            return result;
        }
    }
}
