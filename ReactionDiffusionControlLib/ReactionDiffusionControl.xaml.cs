///////////////////////////////////////////////////////////////////////////////
//
// ReactionDiffusionControl.xaml.cs
// by James P. Galasyn (jgalasyn@microsoft.com)
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using ReactionDiffusionLib;

namespace ReactionDiffusionControlLib
{
    /// <summary>
    /// A control that displays a ReactionVessel buffer. 
    /// </summary>
    /// <remarks>
    /// The ReactionDiffusionControl provides a thin wrapper around an
    /// instance of the ReactionVessel class. A worker thread repeatedly
    /// calls the ComputeReaction method, and the ReactionVessel's out
    /// buffer is marshalled to the UI thread for display in a 
    /// WriteableBitmap.
    /// </remarks>
    public partial class ReactionDiffusionControl : UserControl
    {
        ReactionVessel _reactionVessel = null;
        BackgroundWorker _backgroundWorker = null;
        WriteableBitmap _bitmap = null;
        
        private int indexA = 0;
        private int indexB = 1;
        private int indexC = 2;

        double injectXa = 0.33;
        double injectXb = 0.33;
        double injectXc = 0.33;

        bool dragging = false;

        public ReactionDiffusionControl()
        {
            InitializeComponent();

            this._reactionVessel = new ReactionVessel();
        }

        /// <summary>
        /// Starts the worker thread.
        /// </summary>
        public void Start()
        {
            if (!this._backgroundWorker.IsBusy)
            {
                this._backgroundWorker.RunWorkerAsync(this._reactionVessel);
            }
        }

        /// <summary>
        /// Stops the worker thread.
        /// </summary>
        public void Stop()
        {
            if (this._backgroundWorker.IsBusy && !this._backgroundWorker.CancellationPending)
            {
                this._backgroundWorker.CancelAsync();
            }
        }

        /// <summary>
        /// Initializes the control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>
        /// The Loaded event handler creates a BackgroundWorker and a 
        /// WriteableBitmap. The bitmap is displayed in an Image control. 
        /// </remarks>
        private void UserControl_Loaded( object sender, RoutedEventArgs e )
        {  
            this._backgroundWorker = new BackgroundWorker();
            this._backgroundWorker.WorkerReportsProgress = true;
            this._backgroundWorker.WorkerSupportsCancellation = true;
            this._backgroundWorker.DoWork += new DoWorkEventHandler( backgroundWorker_DoWork );
            this._backgroundWorker.ProgressChanged += new ProgressChangedEventHandler( backgroundWorker_ProgressChanged );

            this._bitmap = new WriteableBitmap(
                (int)this.ActualWidth,
                (int)this.ActualHeight,
                96,
                96,
                PixelFormats.Bgr32,
                null );

            this._bitmapImage.Source = this._bitmap;

            this._reactionVessel.ResizeVessel( (int)this.ActualWidth, (int)this.ActualHeight );
        }

        /// <summary>
        /// Continuously calls the ComputeReaction method, until interrupted
        /// by a call to the Stop method.
        /// </summary>
        void backgroundWorker_DoWork( object sender, DoWorkEventArgs e )
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            ReactionVessel rv = e.Argument as ReactionVessel;

            double[, ,] reaction = null;

            while( !bw.CancellationPending )
            {
                reaction = rv.ComputeReaction();

                bw.ReportProgress( 0, reaction );
            }
        }

        /// <summary>
        /// Called when a frame is completed.
        /// </summary>
        /// <remarks>
        /// The BackgroundWorker class takes care of marshalling the array
        /// from the worker thread to the UI thread.
        /// </remarks>
        void backgroundWorker_ProgressChanged( object sender, ProgressChangedEventArgs e )
        {
            double[, ,] reaction = (double[, ,])e.UserState;
            this.renderReaction( reaction );
        }

        /// <summary>
        /// Converts the computed reaction array into an array of pixels to be rendered.
        /// </summary>
        /// <param name="reaction">The array containing the computed reaction.</param>
        /// <remarks>
        /// The results of computing the reaction are stored in a 3D double array. The 
        /// WriteableBitmap class requires a byte array, so this method copies and converts
        /// each element from the 3D array to the 1D array.
        /// </remarks>
        private void renderReaction(double[, ,] reaction)
        {
            int width = this._reactionVessel.vesselWidth;
            int height = this._reactionVessel.vesselHeight;

            int stride = width * ((this._bitmap.Format.BitsPerPixel + 7) / 8);
            int arraySize = stride * height;
            byte[] pixels = new byte[arraySize];
            int index = 0;

            double Xa = 0;
            double Xb = 0;
            double Xc = 0;

            // Copy the double array to the pixel array.
            // Note: Assumes Xa, Xb, and Xc are all in the
            // range [0,1].
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Xa = reaction[indexA, i, j];
                    Xb = reaction[indexB, i, j];
                    Xc = reaction[indexC, i, j];

                    pixels[index++] = (byte)(Xa * 255);
                    pixels[index++] = (byte)(Xb * 255);
                    pixels[index++] = (byte)(Xc * 255);
                    pixels[index++] = 255;
                }
            }

            // Set up the source rect.
            Int32Rect rect = new Int32Rect(0, 0, this._bitmap.PixelWidth, this._bitmap.PixelHeight);

            // Send the pixel array to the rendering system. 
            this._bitmap.WritePixels(rect, pixels, stride, 0);
        }

        ///////////////////////////////////////////////////////////////////////
        #region Public Properties

        public double K1
        {
            get
            {
                return this._reactionVessel.K1;
            }

            set
            {
                if( this._reactionVessel.K1 != value )
                {
                    this._reactionVessel.K1 = value;
                }
            }
        }

        public double K2
        {
            get
            {
                return this._reactionVessel.K2;
            }

            set
            {
                if( this._reactionVessel.K2 != value )
                {
                    this._reactionVessel.K2 = value;
                }
            }
        }

        public double W1
        {
            get
            {
                return this._reactionVessel.W1;
            }

            set
            {
                if( this._reactionVessel.W1 != value )
                {
                    this._reactionVessel.W1 = value;
                }
            }
        }

        public double W2
        {
            get
            {
                return this._reactionVessel.W2;
            }

            set
            {
                if( this._reactionVessel.W2 != value )
                {
                    this._reactionVessel.W2 = value;
                }
            }
        }

        public double K1Max
        {
            get
            {
                return ReactionVessel.K1Max;
            }
        }

        public double K1Min
        {
            get
            {
                return ReactionVessel.K1Min;
            }
        }


        public double K2Max
        {
            get
            {
                return ReactionVessel.K2Max;
            }
        }

        public double K2Min
        {
            get
            {
                return ReactionVessel.K2Min;
            }
        }

        public double W1Max
        {
            get
            {
                return ReactionVessel.W1Max;
            }
        }

        public double W1Min
        {
            get
            {
                return ReactionVessel.W1Min;
            }
        }

        public double W2Max
        {
            get
            {
                return ReactionVessel.W2Max;
            }
        }

        public double W2Min
        {
            get
            {
                return ReactionVessel.W2Min;
            }
        }

        public BoundaryConditions BoundaryConditions
        {
            get
            {
                return this._reactionVessel.BoundaryConditions;
            }

            set
            {
                if( this._reactionVessel.BoundaryConditions != value )
                {
                    this._reactionVessel.BoundaryConditions = value;
                }
            }
        }

        #endregion

        protected override void OnMouseDown( MouseButtonEventArgs e )
        { 
            double mouseX = e.GetPosition( this ).X;
            double mouseY = e.GetPosition( this ).Y;

            if( ( mouseX <= this.ActualWidth ) &&
                ( mouseY <= this.ActualHeight ) &&
                ( mouseX <= this._reactionVessel.vesselWidth ) &&
                ( mouseY <= this._reactionVessel.vesselHeight ) )
            {
                this.dragging = true;

                this._reactionVessel.Inject( 
                    (int)mouseX, 
                    (int)mouseY, 
                    injectXa,
                    injectXb,
                    injectXc);
            }
        }

        protected override void OnMouseUp( MouseButtonEventArgs e )
        {
            this.dragging = false;
        }

        protected override void OnMouseMove( MouseEventArgs e )
        {   
            if( this.dragging )
            {
                double mouseX = e.GetPosition( this ).X;
                double mouseY = e.GetPosition( this ).Y;

                if( ( mouseX <= this.ActualWidth &&  
                    ( mouseY <= this.ActualHeight ) ) ) 
                {
                    this._reactionVessel.Inject( 
                        (int)mouseY, 
                        (int)mouseX,
                        injectXa,
                        injectXb,
                        injectXc);
                }
            }
        }

        public event IterationCompletedEventHandler IterationCompleted
        {
            add
            {
                this._reactionVessel.IterationCompleted += value;
            }

            remove
            {
                this._reactionVessel.IterationCompleted -= value;
            }
        }
    }
}
