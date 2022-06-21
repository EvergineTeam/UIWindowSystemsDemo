using Evergine.Forms;
using System;
using System.Windows.Forms;
using Application = Evergine.Framework.Application;

namespace UIWindowSystemsDemo.Windows
{
    public partial class CustomForm : Form
    {
        private InteractionService interactionService;

        public CustomForm()
        {
            InitializeComponent();

        }

        public void Initialize(Application application)
        {
            interactionService = new InteractionService();
            application.Container.RegisterInstance(interactionService);
        }

        public void SetEvergineControl(EvergineControl control)
        {
            control.Dock = DockStyle.Fill;
            evergineContainer.Controls.Add(control);
        }

        private void btnCameraReset_Click(object sender, EventArgs e) =>
            interactionService?.ResetCamera();

        private void tbDisplacement_ValueChanged(object sender, EventArgs e)
        {
            if (sender is TrackBar trackBar)
            {
                interactionService.Displacement = trackBar.Value;
            }
        }
    }
}
