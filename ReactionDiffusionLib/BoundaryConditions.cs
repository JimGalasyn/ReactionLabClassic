using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReactionDiffusionLib
{
    public enum BoundaryConditions
    {
        RVBC_None,
        RVBC_Uniform,
        RVBC_XGradient,
        RVBC_YGradient,
        RVBC_XSine,
        RVBC_YSine,
        RVBC_XRamp,
        RVBC_YRamp
    }
}
