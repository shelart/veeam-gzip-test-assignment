using System;
using System.Collections.Generic;
using System.Text;

namespace VeeamTestAssignmentGzip
{
    public static class MathUtils
    {
        public static long DivideRoundUp(this long total, long divider) => (total + divider - 1) / divider;
    }
}
