using System;
using System.Collections.Generic;
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
using System.Windows.Threading;

namespace ReactionDiffusionLab
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        DateTime _startFrameTime;

        public Window1()
        {
            InitializeComponent();
        }

        private void startButton_Click( object sender, RoutedEventArgs e )
        {
            this._startFrameTime = DateTime.Now;

            this._reactionDiffusionControl.Start();
        }

        private void stopButton_Click( object sender, RoutedEventArgs e )
        {
            this._reactionDiffusionControl.Stop(); 
        }

        private void radioButton_Checked( object sender, RoutedEventArgs e )
        {
            if( sender == this.rbUniform )
            {
                this._reactionDiffusionControl.BoundaryConditions = BoundaryConditions.RVBC_Uniform;
            }
            else if( sender == this.rbXGradient )
            {
                this._reactionDiffusionControl.BoundaryConditions = BoundaryConditions.RVBC_XGradient;
            }
            else if( sender == this.rbYGradient )
            {
                this._reactionDiffusionControl.BoundaryConditions = BoundaryConditions.RVBC_YGradient;
            }
            else if( sender == this.rbXRamp )
            {
                this._reactionDiffusionControl.BoundaryConditions = BoundaryConditions.RVBC_XRamp;
            }
            else if( sender == this.rbYRamp )
            {
                this._reactionDiffusionControl.BoundaryConditions = BoundaryConditions.RVBC_YRamp;
            }
            else if( sender == this.rbXSine )
            {
                this._reactionDiffusionControl.BoundaryConditions = BoundaryConditions.RVBC_XSine;
            }
            else if( sender == this.rbYSine )
            {
                this._reactionDiffusionControl.BoundaryConditions = BoundaryConditions.RVBC_YSine;
            }   
        }

        private void reactionDiffusionControl_IterationCompleted( object sender, IterationCompletedEventArgs e )
        {
            
            TimeSpan frameTime = DateTime.Now - this._startFrameTime;

            this.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new DispatcherOperationCallback(
                        delegate( object param )
                        {
                            this.textBlock6.Text = param as string;

                            return null;

                        } ), frameTime.TotalMilliseconds.ToString() );

            this._startFrameTime = DateTime.Now;

        }
    }
}
