namespace UIWindowSystemsDemo.MAUI.Evergine
{
    public partial class EvergineViewHandler
    {
        public static IPropertyMapper<EvergineView, EvergineViewHandler> PropertyMapper = new PropertyMapper<EvergineView, EvergineViewHandler>(ViewMapper)
        {
            [nameof(EvergineView.Application)] = MapApplication,
        };

        public static CommandMapper<EvergineView, EvergineViewHandler> CommandMapper = new(ViewCommandMapper);

        // Unlike the Evergine MAUI template, this sample shows the same Evergine application in several
        // EvergineViews (one display each). The application is initialized once and a single render loop,
        // owned by the first connected handler, updates and draws it; the other handlers only add their display.
        private static readonly List<EvergineViewHandler> connectedHandlers = new();
        private static EvergineViewHandler loopOwner;

        private EvergineView connectedView;
        private bool isReadyNotified;

        public EvergineViewHandler()
            : base(PropertyMapper, CommandMapper)
        {
        }

        private void JoinSharedLoop(EvergineView view)
        {
            this.connectedView = view;
            this.isReadyNotified = false;
            if (!connectedHandlers.Contains(this))
            {
                connectedHandlers.Add(this);
            }
        }

        private void LeaveSharedLoop()
        {
            connectedHandlers.Remove(this);
            if (ReferenceEquals(loopOwner, this))
            {
                loopOwner = null;
            }

            this.connectedView = null;
            this.isReadyNotified = false;
        }

        private static void ResetSharedLoop()
        {
            connectedHandlers.Clear();
            loopOwner = null;
        }

        private static void NotifyReadyHandlers()
        {
            foreach (var handler in connectedHandlers.ToArray())
            {
                if (handler.isEvergineInitialized && !handler.isReadyNotified && handler.connectedView != null)
                {
                    handler.isReadyNotified = true;
                    handler.connectedView.NotifyReady();
                }
            }
        }

        private void RemoveDisplay(EvergineView view)
        {
            var graphicsPresenter = view.Application.Container.Resolve<global::Evergine.Framework.Services.GraphicsPresenter>();
            var display = graphicsPresenter.RemoveDisplay(view.DisplayName);
            if (display == null)
            {
                return;
            }

            if (ReferenceEquals(graphicsPresenter.FocusedDisplay, display))
            {
                graphicsPresenter.FocusedDisplay = null;
            }

            display.Dispose();
        }
    }
}
