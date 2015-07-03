# ReactionLabClassic
Shows how to implement a reaction-diffusion simulation by using WPF and the [WriteableBitmap](https://msdn.microsoft.com/en-us/library/system.windows.media.imaging.writeablebitmap.aspx) class.

For details, see 
[Using WriteableBitmap to Display a Procedural Texture](http://blogs.msdn.com/b/jgalasyn/archive/2008/04/17/using-writeablebitmap-to-display-a-procedural-texture.aspx).

# Notes
- The ReactionDiffusionControl is a XAML control that uses [WriteableBitmap](https://msdn.microsoft.com/en-us/library/system.windows.media.imaging.writeablebitmap.aspx) to render a dynamic texture. In a real-world app, you should probably prefer writing an [HLSL shader](https://msdn.microsoft.com/en-us/library/windows/desktop/bb509635.aspx), which runs on the GPU and is immensely faster.  
- The off-thread work is performed by a [BackgroundWorker](https://msdn.microsoft.com/en-us/library/system.componentmodel.backgroundworker.aspx) instance. Although this works fine, it's an archaic way to do asynchronous operations. Nowadays, you should prefer using the async and await keywords. See [Asynchronous programming (Windows Runtime apps)](https://msdn.microsoft.com/en-us/library/windows/apps/hh464924.aspx) for details.
