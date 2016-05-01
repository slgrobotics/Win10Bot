using slg.RobotBase.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks.Dataflow;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace slg.Display
{
    public sealed partial class RumblePad2Dashboard : UserControl
    {
        public RumblePad2Dashboard()
        {
            dataBlock = new ActionBlock<IJoystickSubState>(v => { Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { JoystickDataText = v.ToString(); }).AsTask().Wait(); });

            this.InitializeComponent();
        }

        private ActionBlock<IJoystickSubState> dataBlock;

        public ActionBlock<IJoystickSubState> JoystickDataBlock { get { return dataBlock; } }

        public string JoystickDataText { get { return dataLabel.Text; } private set { dataLabel.Text = value; } }
    }
}
