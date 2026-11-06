using EvergineApplication = global::Evergine.Framework.Application;

namespace UIWindowSystemsDemo.MAUI.Evergine
{
    public class EvergineView : View
    {
        private readonly object readinessSync = new();
        private TaskCompletionSource readyCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool isReady;

        public static readonly BindableProperty ApplicationProperty =
            BindableProperty.Create(nameof(Application), typeof(EvergineApplication), typeof(EvergineView), null);

        public static readonly BindableProperty DisplayNameProperty =
            BindableProperty.Create(nameof(DisplayName), typeof(string), typeof(EvergineView), "DefaultDisplay");

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

        public Task WaitUntilReadyAsync(CancellationToken cancellationToken)
        {
            lock (this.readinessSync)
            {
                return this.isReady
                    ? Task.CompletedTask
                    : this.readyCompletion.Task.WaitAsync(cancellationToken);
            }
        }

        internal void NotifyReady()
        {
            TaskCompletionSource readyToComplete;
            lock (this.readinessSync)
            {
                if (this.isReady)
                {
                    return;
                }

                this.isReady = true;
                readyToComplete = this.readyCompletion;
            }

            readyToComplete.TrySetResult();
        }

        internal void NotifyDisconnected()
        {
            lock (this.readinessSync)
            {
                this.isReady = false;
                if (this.readyCompletion.Task.IsCompleted)
                {
                    this.readyCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                }
            }
        }

        internal void StartInteraction() => this.PointerPressed?.Invoke(this, EventArgs.Empty);

        internal void MovedInteraction() => this.PointerMoved?.Invoke(this, EventArgs.Empty);

        internal void EndInteraction() => this.PointerReleased?.Invoke(this, EventArgs.Empty);
    }
}
