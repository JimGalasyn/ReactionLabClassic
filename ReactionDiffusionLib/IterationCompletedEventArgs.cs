using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReactionDiffusionLib
{
    public class IterationCompletedEventArgs : EventArgs
    {
        private int iterationValue;

        public IterationCompletedEventArgs(int iteration)
        {
            if (iteration < 0)
            {
                throw new ArgumentException("must be >= 0", "iteration");
            }

            this.iterationValue = iteration;
        }


        public int Iteration
        {
            get
            {
                return this.iterationValue;
            }
        }
    } 
}
