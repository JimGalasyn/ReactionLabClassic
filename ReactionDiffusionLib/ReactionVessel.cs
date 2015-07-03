///////////////////////////////////////////////////////////////////////////////
//
// ReactionVessel.cs
// by James P. Galasyn (jgalasyn@microsoft.com)
//

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;


namespace ReactionDiffusionLib
{
    public delegate void IterationCompletedEventHandler(object sender, IterationCompletedEventArgs e);

    /// <summary>
    /// Simulates a 2D reaction-diffusion system. 
    /// </summary>
    /// <remarks>
    /// The ReactionVessel class is an implementation of the 
    /// reaction-diffusion model described in [1]. Use this
    /// class as a procedural texture to produce many different 
    /// animated effects.
    /// 
    /// The chemical dynamics are modeled in the ComputeReaction
    /// method. The reaction is modeled as a three-dimensional array.
    /// Each layer  
    /// 
    /// [1] V. G. Gontar and A. V. Il'in, "New dynamic model describing 
    /// spatio-temporal behaviour of chemical reactions," 
    /// Physica D 52 (1991), pp 528-531.
    /// </remarks>
    public class ReactionVessel
    {
        // For fast native copy of buffers.
        [DllImport("kernel32.dll")]
        public static unsafe extern int CopyMemory(void* pDest, void* pSrc, uint length);


        ////////////////////////////////////////////////////////////////////////////////
        #region Instance Variables

        public int vesselWidth = vesselWidthDefault;
        public int vesselHeight = vesselHeightDefault;
        public int iteration = 0;
        public double b = bDefault;
        public double K1 = K1Default;
        public double W1 = W1Default;
        public double K2 = K2Default;
        public double W2 = W2Default;

        private unsafe double[, ,] reaction = null;
        private unsafe double[, ,] reactionOut = null;
        private int indexA = 0;
        private int indexB = 1;
        private int indexC = 2;

        private BoundaryConditions _boundaryConditions;
        private BoundaryConditions _boundaryConditionsPrev = BoundaryConditions.RVBC_None;
        private static double minConcentration = 0.001;

        #endregion

        ////////////////////////////////////////////////////////////////////////////////
        #region Constructors

        public ReactionVessel()
        {
            Init();
        }

        public ReactionVessel(
            int vesselWidth,
            int vesselHeight,
            double b,
            double K1,
            double W1,
            double K2,
            double W2)
        {
            try
            {
                Init(
                    vesselWidth,
                    vesselHeight,
                    b,
                    K1,
                    W1,
                    K2,
                    W2);
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("Bad parameter: " + e.ToString());
            }
        }

        #endregion

        ////////////////////////////////////////////////////////////////////////////////
        #region Public Methods

        /// <summary>
        /// Computes one frame of the reaction.
        /// </summary>
        /// <returns>An array containing the reaction products for the next frame. </returns>
        public unsafe double[, ,] ComputeReaction()
        {
            Debug.Assert(CheckInvariant(), "ComputeReaction: CheckInvariant failed");

            double Xa;
            double Xb;
            double Xc;
            double c;
            double d;
            double f;
            double g0;
            double feedbackFromC;
            double feedbackFromB;

            iteration++;

            // If boundary conditions have changed since the previous frame, update them.
            if (this._boundaryConditionsPrev != this._boundaryConditions)
            {
                this.SetBoundaryConditions(this._boundaryConditions);

                this._boundaryConditionsPrev = this._boundaryConditions;
            }

            // Copy boundary values to the out buffer.
            for (int i = 0; i < vesselWidth; i++)
            {
                reactionOut[indexA, i, 0] = reaction[indexA, i, 0];
                reactionOut[indexB, i, 0] = reaction[indexB, i, 0];
                reactionOut[indexC, i, 0] = reaction[indexC, i, 0];

                reactionOut[indexA, i, vesselHeight - 1] = reaction[indexA, i, vesselHeight - 1];
                reactionOut[indexB, i, vesselHeight - 1] = reaction[indexB, i, vesselHeight - 1];
                reactionOut[indexC, i, vesselHeight - 1] = reaction[indexC, i, vesselHeight - 1];
            }

            for (int j = 0; j < vesselHeight; j++)
            {
                reactionOut[indexA, 0, j] = reaction[indexA, 0, j];
                reactionOut[indexB, 0, j] = reaction[indexB, 0, j];
                reactionOut[indexC, 0, j] = reaction[indexC, 0, j];

                reactionOut[indexA, vesselWidth - 1, j] = reaction[indexA, vesselWidth - 1, j];
                reactionOut[indexB, vesselWidth - 1, j] = reaction[indexB, vesselWidth - 1, j];
                reactionOut[indexC, vesselWidth - 1, j] = reaction[indexC, vesselWidth - 1, j];
            }

            // Traverse the grid and compute concentrations for
            // all three reactants. This implements the Gontar model.
            for (int i = 1; i < vesselWidth - 1; i++)
            {
                for (int j = 1; j < vesselHeight - 1; j++)
                {
                    try
                    {
                        // Compute concentrations at cell (i,j). 
                        feedbackFromC = Math.Exp( -W1 / weight( indexC, i, j ) );
                        feedbackFromB = Math.Exp( -W2 * weight( indexB, i, j ) );
                        g0 = ( K1 * K2 * feedbackFromC * feedbackFromB ) / ( 1.0 + K1 * feedbackFromC );

                        // Compute concentrations at cell (i,j).
                        //c = -W1 / weight(2, i, j);
                        //f = -W2 * weight(1, i, j);
                        //e_to_c = Math.Exp(c);
                        //e_to_f = Math.Exp(f);
                        //d = 1.0 + K1 * e_to_c;
                        //g0 = (K1 * K2 * e_to_c * e_to_f) / d;

                        Xc = b * (g0 / (g0 + 1));
                        if (Xc <= minConcentration) Xc = minConcentration;

                        Xb = (K1 * feedbackFromC / (1 + K1 * feedbackFromC)) * (b - Xc);
                        if (Xb <= minConcentration) Xb = minConcentration;

                        Xa = b - Xb - Xc;
                        if (Xa <= minConcentration) Xa = minConcentration;


                        if (Xa > b) Xa = b;

                        if (Xb > b) Xb = b;

                        if (Xc > b) Xc = b;

                        reactionOut[indexA, i, j] = Xa;
                        reactionOut[indexB, i, j] = Xb;
                        reactionOut[indexC, i, j] = Xc;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("caught exception: ", ex.ToString());
                    }

                }
            }

            int ubi = reaction.GetUpperBound(1) + 1;
            int ubj = reaction.GetUpperBound(2) + 1;

            // Copy the out buffer to the in buffer.
            fixed (double* pOutBuff = reactionOut, pInBuff = reaction)
            {
                uint buffSize = (uint)(3 * ubi * ubj * sizeof(double));

                uint count = buffSize;

                double* ps = pOutBuff;
                double* pd = pInBuff;

                lock (reactionOut.SyncRoot)
                {
                    try
                    {
                        CopyMemory((void*)pd, (void*)ps, count);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message);
                    }
                }
            }

            // Raise the IterationCompleted event.
            IterationCompletedEventArgs e = new IterationCompletedEventArgs(this.iteration);
            OnInterationCompleted(e);

            return reactionOut;
        }

        /// <summary>
        /// Changes the reaction vessels dimensions.
        /// </summary>
        /// <param name="width">The new width of the reaction vessel.</param>
        /// <param name="height">The new height of the reaction vessel.</param>
        public void ResizeVessel(int width, int height)
        {
            CheckParamVesselWidth(width);
            CheckParamVesselHeight(height);

            // Realloc the in and out buffers.
            reaction = new double[3, width, height];
            reactionOut = new double[3, width, height];

            // Cache the dimensions.
            vesselWidth = width;
            vesselHeight = height;

            // Set up the initial conditions.
            this.SetInitialConditions();
        }

        /// <summary>
        /// Sets the given reactant concentrations at the given position.
        /// </summary>
        /// <param name="posX">The x-axis position of the injection.</param>
        /// <param name="posY">The y-axis position of the injection.</param>
        /// <param name="Xa">The concentration of chemical A to inject.</param>
        /// <param name="Xb">The concentration of chemical B to inject.</param>
        /// <param name="Xc">The concentration of chemical C to inject.</param>
        public void Inject(int posX, int posY, double Xa, double Xb, double Xc)
        {
            // Clamp inputs.
            if (posX < 1) posX = 1;
            if (posX > vesselWidth - 1) posX = vesselWidth - 1;
            if (posY < 1) posY = 1;
            if (posY > vesselWidth - 1) posY = vesselWidth - 1;

            // Set the concentration of chemical A in the neighboring cells. 
            reaction[indexA, posX - 1, posY - 1] = Xa;
            reaction[indexA, posX, posY - 1] = Xa;
            reaction[indexA, posX + 1, posY - 1] = Xa;
            reaction[indexA, posX - 1, posY] = Xa;
            reaction[indexA, posX, posY] = Xa;
            reaction[indexA, posX + 1, posY] = Xa;
            reaction[indexA, posX - 1, posY + 1] = Xa;
            reaction[indexA, posX, posY + 1] = Xa;
            reaction[indexA, posX + 1, posY + 1] = Xa;

            // Set the concentration of chemical B in the neighboring cells. 
            reaction[indexB, posX - 1, posY - 1] = Xb;
            reaction[indexB, posX, posY - 1] = Xb;
            reaction[indexB, posX + 1, posY - 1] = Xb;
            reaction[indexB, posX - 1, posY] = Xb;
            reaction[indexB, posX, posY] = Xb;
            reaction[indexB, posX + 1, posY] = Xb;
            reaction[indexB, posX - 1, posY + 1] = Xb;
            reaction[indexB, posX, posY + 1] = Xb;
            reaction[indexB, posX + 1, posY + 1] = Xb;

            // Set the concentration of chemical C in the neighboring cells. 
            reaction[indexC, posX - 1, posY - 1] = Xc;
            reaction[indexC, posX, posY - 1] = Xc;
            reaction[indexC, posX + 1, posY - 1] = Xc;
            reaction[indexC, posX - 1, posY] = Xc;
            reaction[indexC, posX, posY] = Xc;
            reaction[indexC, posX + 1, posY] = Xc;
            reaction[indexC, posX - 1, posY + 1] = Xc;
            reaction[indexC, posX, posY + 1] = Xc;
            reaction[indexC, posX + 1, posY + 1] = Xc;
        }

        #endregion

        ///////////////////////////////////////////////////////////////////////
        #region Public Properties

        /// <summary>
        /// Gets or sets the boundary conditions for the reaction vessel.
        /// </summary>
        public BoundaryConditions BoundaryConditions
        {
            get
            {
                return this._boundaryConditions;
            }

            set
            {
                if (this._boundaryConditions != value)
                {
                    this._boundaryConditions = value;

                    this.SetBoundaryConditions(this._boundaryConditions);
                }
            }
        }

        #endregion

        ///////////////////////////////////////////////////////////////////////
        #region Public Events

        /// <summary>
        /// Occurs when a frame has been computed.
        /// </summary>
        public event IterationCompletedEventHandler IterationCompleted;

        #endregion

        ////////////////////////////////////////////////////////////////////////////////
        #region Implementation

        /// <summary>
        /// Averages the concentration of a reactant among the 
        /// neighbors of cell (i,j). 
        /// </summary>
        /// <param name="X">Index of a reactant in the reaction array.</param>
        /// <param name="i">i index of cell at (i,j).</param>
        /// <param name="j">j index of cell at (i,j).</param>
        /// <returns>The concentration of X, averaged over neighoring cells.</returns>
        /// <remarks>
        /// Array access is implemented with pointer math in an unsafe block. 
        /// Presumably, this is faster than the usual managed access through
        /// properties, but I haven't profiled it.
        /// </remarks>
        private unsafe double weight(int X, int i, int j)
        {
            double M = 0;

            double* pElement = null;

            double d1 = 0;
            double d2 = 0;
            double d3 = 0;
            double d4 = 0;
            double d5 = 0;
            double d6 = 0;
            double d7 = 0;
            double d8 = 0;
            double d9 = 0;
            int iMax = this.reaction.GetUpperBound(1) + 1;
            int jMax = this.reaction.GetUpperBound(2) + 1;
            int xOffset = (X * iMax * jMax);

            fixed (double* pBase = reaction)
            {
                d1 = *(pBase + (xOffset + (i - 1) * jMax + j - 1));
                d2 = *(pBase + (xOffset + (i) * jMax + j - 1));
                d3 = *(pBase + (xOffset + (i + 1) * jMax + j - 1));
                d4 = *(pBase + (xOffset + (i - 1) * jMax + j));
                d5 = *(pBase + (xOffset + (i) * jMax + j));
                d6 = *(pBase + (xOffset + (i + 1) * jMax + j));
                d7 = *(pBase + (xOffset + (i - 1) * jMax + j + 1));
                d8 = *(pBase + (xOffset + (i) * jMax + j + 1));
                d9 = *(pBase + (xOffset + (i + 1) * jMax + j + 1));

                M = (d1 + d2 + d3 + d4 + d5 + d6 + d7 + d8 + d9) / 9;
            }

            return M;

        }


        protected virtual void Init()
        {
            this.vesselWidth = vesselWidthDefault;
            this.vesselHeight = vesselHeightDefault;
            this.b = bDefault;
            this.K1 = K1Default;
            this.W1 = W1Default;
            this.K2 = K2Default;
            this.W2 = W2Default;

            ResizeVessel(vesselWidth, vesselHeight);
        }

        protected virtual void Init(
            int vesselWidth,
            int vesselHeight,
            double b,
            double K1,
            double W1,
            double K2,
            double W2)
        {
            // Check inputs.
            try
            {
                CheckReactionParameters(
                    vesselWidth,
                    vesselHeight,
                    b,
                    K1,
                    W1,
                    K2,
                    W2);
            }
            catch (Exception e)
            {
                Console.WriteLine("Bad parameter: " + e.ToString());
                throw e;
            }

            // Assign instance variables.
            this.vesselWidth = vesselWidth;
            this.vesselHeight = vesselHeight;
            this.b = b;
            this.K1 = K1;
            this.W1 = W1;
            this.K2 = K2;
            this.W2 = W2;

            ResizeVessel(vesselWidth, vesselHeight);
        }
        

        /// <summary>
        /// Raises the InterationCompleted event.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnInterationCompleted(IterationCompletedEventArgs e)
        {
            Trace.Assert(e != null);

            if (IterationCompleted != null)
            {
                IterationCompleted(this, e);
            }
        }


        /// <summary>
        /// Assigns initial conditions for the reaction vessel.
        /// </summary>
        private void SetInitialConditions()
        {
            this.SetBoundaryConditions(BoundaryConditions.RVBC_Uniform);

            this._boundaryConditionsPrev = this._boundaryConditions;

            for (int i = 1; i < vesselWidth - 1; i++)
            {
                for (int j = 1; j < vesselHeight - 1; j++)
                {
                    reaction[indexA, i, j] = initialXa;
                    reaction[indexB, i, j] = initialXb;
                    reaction[indexC, i, j] = initialXc;
                }
            }
        }

        private void SetBoundaryConditions(BoundaryConditions boundary)
        {
            lock (this.reaction.SyncRoot)
            {
                for (int i = 0; i < vesselWidth; i++)
                {
                    this.SetBoundaryCondition(i, 0, boundary);
                    this.SetBoundaryCondition(i, vesselHeight - 1, boundary);
                }

                for (int j = 0; j < vesselHeight; j++)
                {
                    this.SetBoundaryCondition(0, j, boundary);
                    this.SetBoundaryCondition(vesselWidth - 1, j, boundary);
                }
            }
        }

        private void SetBoundaryCondition(int i, int j, BoundaryConditions boundary)
        {
            switch (boundary)
            {
                case BoundaryConditions.RVBC_Uniform:
                {   
                    reaction[indexA, i, j] = initialXa;
                    reaction[indexB, i, j] = initialXb;
                    reaction[indexC, i, j] = initialXc;

                    break;
                }
                case BoundaryConditions.RVBC_XGradient:
                {
                    double d = (double)j / (double)vesselHeight;

                    reaction[indexA, i, j] = d;
                    reaction[indexB, i, j] = 1 - d;
                    reaction[indexC, i, j] = minConcentration;

                    break;
                }
                case BoundaryConditions.RVBC_YGradient:
                {
                    double d = (double)i / (double)vesselWidth;

                    reaction[indexA, i, j] = d;
                    reaction[indexB, i, j] = 1 - d;
                    reaction[indexC, i, j] = minConcentration;

                    break;
                }

                case BoundaryConditions.RVBC_XSine:
                {
                    double freq = 2.0;
                    double d = Math.Abs(Math.Sin(freq * (double)j * (Math.PI / 180)));

                    reaction[indexA, i, j] = 1 - d;
                    reaction[indexB, i, j] = d; // 1 - d;
                    reaction[indexC, i, j] = minConcentration;

                    break;
                }
                case BoundaryConditions.RVBC_YSine:
                {
                    double freq = 2.0;
                    double d = Math.Abs(Math.Sin(freq * (double)i * (Math.PI / 180)));

                    reaction[indexA, i, j] = 1 - d;
                    reaction[indexB, i, j] = d; // 1 - d;
                    reaction[indexC, i, j] = minConcentration;

                    break;
                }

                default:
                {
                    break;
                }
            }
        }

        #endregion


        ///////////////////////////////////////////////////////////////////////
        #region Parameter Checking

        public bool CheckInvariant()
        {
            bool bSuccess = false;

            if ((reaction != null) &&
                (reactionOut != null) &&
                (vesselWidth >= vesselWidthMin) &&
                (vesselWidth <= vesselWidthMax) &&
                (vesselHeight >= vesselHeightMin) &&
                (vesselHeight <= vesselHeightMax) &&
                (b >= bMin) &&
                (b <= bMax) &&
                (K1 >= K1Min) &&
                (K1 <= K1Max) &&
                (W1 >= W1Min) &&
                (W1 <= W1Max) &&
                (W2 >= W2Min) &&
                (W2 <= W2Max))
            {
                bSuccess = true;
            }

            return bSuccess;
        }


        public static void CheckReactionParameters(
            int vesselWidth,
            int vesselHeight,
            double b,
            double K1,
            double W1,
            double K2,
            double W2)
        {
            CheckParamVesselWidth(vesselWidth);
            CheckParamVesselHeight(vesselHeight);
            CheckParamB(b);
            CheckParamK1(K1);
            CheckParamW1(W1);
            CheckParamK2(K2);
            CheckParamW2(W2);
        }

        public static void CheckParamVesselWidth(int vesselWidth)
        {
            if ((vesselWidth < vesselWidthMin) || (vesselWidth > vesselWidthMax))
            {
                throw new ArgumentException("out of range", "vesselWidth");
            }
        }

        public static void CheckParamVesselHeight(int vesselHeight)
        {
            if ((vesselHeight < vesselHeightMin) || (vesselHeight > vesselHeightMax))
            {
                throw new ArgumentException("out of range", "vesselHeight");
            }
        }

        public static void CheckParamB(double b)
        {
            if ((b < bMin) || (b > bMax))
            {
                throw new ArgumentException("out of range", "b");
            }
        }

        public static void CheckParamK1(double K1)
        {
            if ((K1 < K1Min) || (K1 > K1Max))
            {
                throw new ArgumentException("out of range", "K1");
            }
        }

        public static void CheckParamW1(double W1)
        {
            if ((W1 < W1Min) || (W1 > W1Max))
            {
                throw new ArgumentException("out of range", "W1");
            }
        }

        public static void CheckParamK2(double K2)
        {
            if ((K2 < K2Min) || (K2 > K2Max))
            {
                throw new ArgumentException("out of range", "K2");
            }
        }

        public static void CheckParamW2(double W2)
        {
            if ((W2 < W2Min) || (W2 > W2Max))
            {
                throw new ArgumentException("out of range", "W2");
            }
        }

        #endregion

        ///////////////////////////////////////////////////////////////////////
        #region Default Instance Values

        public static int vesselWidthDefault = 200;
        public static int vesselHeightDefault = 200;
        public static double bDefault = 1.0;
        public static double K1Default = 5.1;
        public static double W1Default = 0.45;
        public static double K2Default = 5.5;
        public static double W2Default = 6.5;

        public static double initialXa = 0.0;
        public static double initialXb = 0.4;
        public static double initialXc = 0.9;

        #endregion

        ///////////////////////////////////////////////////////////////////////
        #region Instance Value Ranges

        public static int vesselWidthMin = 3;
        public static int vesselWidthMax = 1000;
        public static int vesselHeightMin = 3;
        public static int vesselHeightMax = 1000;
        public static double bMin = 0.1;
        public static double bMax = 2.0;
        public static double K1Min = 0.1;
        public static double K1Max = 100.0;
        public static double W1Min = 0.01;
        public static double W1Max = 10.0;
        public static double K2Min = 0.1;
        public static double K2Max = 100.0;
        public static double W2Min = 0.1;
        public static double W2Max = 100.0;

        #endregion
    }
}
