using EvergineApplication = global::Evergine.Framework.Application;

namespace UIWindowSystemsDemo.MAUI.Evergine
{
    public class EvergineView : View
    {
        public static readonly BindableProperty ApplicationProperty =
            BindableProperty.Create(nameof(Application), typeof(EvergineApplication), typeof(EvergineView), null);

        public static readonly BindableProperty DisplayNameProperty =
            BindableProperty.Create(nameof(DisplayName), typeof(string), typeof(EvergineView), string.Empty);

        public EvergineApplication Application
        {
            get { return (EvergineApplication)this.GetValue(ApplicationProperty); }
            set { this.SetValue(ApplicationProperty, value); }
        }

        public string DisplayName
        {
            get { return (string)this.GetValue(DisplayNameProperty); }
            set { this.SetValue(DisplayNameProperty, value); }
        }

        public event EventHandler PointerPressed;

        public event EventHandler PointerMoved;

        public event EventHandler PointerReleased;

        internal void StartInteraction() => this.PointerPressed?.Invoke(this, EventArgs.Empty);

        internal void MovedInteraction() => this.PointerMoved?.Invoke(this, EventArgs.Empty);

        internal void EndInteraction() => this.PointerReleased?.Invoke(this, EventArgs.Empty);
    }
}
