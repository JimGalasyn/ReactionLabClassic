# ReactionLabClassic
Shows how to implement a reaction-diffusion simulation by using WPF and the [WriteableBitmap](https://msdn.microsoft.com/en-us/library/system.windows.media.imaging.writeablebitmap.aspx) class.

For details, see: 
[Using WriteableBitmap to Display a Procedural Texture](http://blogs.msdn.com/b/jgalasyn/archive/2008/04/17/using-writeablebitmap-to-display-a-procedural-texture.aspx)

# Notes
The ReactionDiffusionControl is a XAML control that uses [WriteableBitmap](https://msdn.microsoft.com/en-us/library/system.windows.media.imaging.writeablebitmap.aspx) to render a dynamic texture.
The off-thread work is performed by a [BackgroundWorker](https://msdn.microsoft.com/en-us/library/system.componentmodel.backgroundworker.aspx) instance. 
