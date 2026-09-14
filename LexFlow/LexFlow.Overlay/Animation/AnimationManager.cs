using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace LexFlow.Overlay.Animation;

/// <summary>
/// Manages smooth animations for the suggestion overlay
/// </summary>
public class AnimationManager
{
    private readonly Dictionary<string, Animation> _animations = new();
    private System.Windows.Forms.Timer _animationTimer = null!;
    
    public event EventHandler? AnimationCompleted;

    public AnimationManager()
    {
        _animationTimer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60 FPS
        _animationTimer.Tick += OnAnimationTick;
    }

    public void Start()
    {
        _animationTimer.Start();
    }

    public void Stop()
    {
        _animationTimer.Stop();
    }

    public void PlayAnimation(string name, Animation animation)
    {
        _animations[name] = animation;
        animation.Start();
    }

    public void StopAnimation(string name)
    {
        if (_animations.TryGetValue(name, out var animation))
        {
            animation.Stop();
            _animations.Remove(name);
        }
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        var completedAnimations = new List<string>();
        
        foreach (var (name, animation) in _animations)
        {
            animation.Update();
            
            if (animation.IsComplete)
            {
                completedAnimations.Add(name);
            }
        }
        
        foreach (var name in completedAnimations)
        {
            _animations.Remove(name);
            AnimationCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}

public abstract class Animation
{
    protected double _progress = 0;
    protected double _duration = 300; // milliseconds
    protected DateTime _startTime;
    protected bool _isRunning = false;
    protected EasingFunction _easing = EasingFunctions.EaseInOutQuad;

    public bool IsComplete => _progress >= 1;
    public double Progress => _progress;

    public virtual void Start()
    {
        _startTime = DateTime.UtcNow;
        _isRunning = true;
        _progress = 0;
    }

    public virtual void Stop()
    {
        _isRunning = false;
    }

    public virtual void Update()
    {
        if (!_isRunning) return;

        var elapsed = (DateTime.UtcNow - _startTime).TotalMilliseconds;
        _progress = Math.Min(elapsed / _duration, 1);
        _progress = _easing(_progress);
    }

    public abstract void Apply(Control control);
}

public class FadeAnimation : Animation
{
    private readonly double _fromOpacity;
    private readonly double _toOpacity;

    public FadeAnimation(double fromOpacity, double toOpacity, double duration = 300)
    {
        _fromOpacity = fromOpacity;
        _toOpacity = toOpacity;
        _duration = duration;
    }

    public override void Apply(Control control)
    {
        if (control is Form form)
        {
            var currentOpacity = _fromOpacity + (_toOpacity - _fromOpacity) * _progress;
            form.Opacity = Math.Max(0, Math.Min(1, currentOpacity));
        }
    }
}

public class SlideAnimation : Animation
{
    private readonly Point _fromPosition;
    private readonly Point _toPosition;

    public SlideAnimation(Point fromPosition, Point toPosition, double duration = 300)
    {
        _fromPosition = fromPosition;
        _toPosition = toPosition;
        _duration = duration;
    }

    public override void Apply(Control control)
    {
        var currentX = _fromPosition.X + (_toPosition.X - _fromPosition.X) * _progress;
        var currentY = _fromPosition.Y + (_toPosition.Y - _fromPosition.Y) * _progress;
        control.Location = new Point((int)currentX, (int)currentY);
    }
}

public class ScaleAnimation : Animation
{
    private readonly float _fromScale;
    private readonly float _toScale;
    private Size _originalSize;

    public ScaleAnimation(float fromScale, float toScale, double duration = 300)
    {
        _fromScale = fromScale;
        _toScale = toScale;
        _duration = duration;
    }

    public override void Apply(Control control)
    {
        if (_originalSize == Size.Empty)
        {
            _originalSize = control.Size;
        }

        var currentScale = _fromScale + (_toScale - _fromScale) * _progress;
        control.Size = new Size(
            (int)(_originalSize.Width * currentScale),
            (int)(_originalSize.Height * currentScale)
        );
    }
}

public class CompositeAnimation : Animation
{
    private readonly List<Animation> _animations = new();

    public void AddAnimation(Animation animation)
    {
        _animations.Add(animation);
    }

    public override void Start()
    {
        base.Start();
        foreach (var animation in _animations)
        {
            animation.Start();
        }
    }

    public override void Update()
    {
        base.Update();
        foreach (var animation in _animations)
        {
            animation.Update();
        }
    }

    public override void Apply(Control control)
    {
        foreach (var animation in _animations)
        {
            animation.Apply(control);
        }
    }
}

public delegate double EasingFunction(double t);

public static class EasingFunctions
{
    public static double Linear(double t) => t;
    
    public static double EaseInQuad(double t) => t * t;
    
    public static double EaseOutQuad(double t) => t * (2 - t);
    
    public static double EaseInOutQuad(double t) => t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
    
    public static double EaseInCubic(double t) => t * t * t;
    
    public static double EaseOutCubic(double t) => (--t) * t * t + 1;
    
    public static double EaseInOutCubic(double t) => t < 0.5 ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1;
    
    public static double EaseOutElastic(double t)
    {
        const double c4 = (2 * Math.PI) / 3;
        return t == 0 ? 0 : t == 1 ? 1 : Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c4) + 1;
    }
}
