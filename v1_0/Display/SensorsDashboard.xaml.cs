using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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

using slg.RobotBase.Interfaces;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace slg.Display
{
    public sealed partial class SensorsDashboard : UserControl
    {
        public SensorsDashboard()
        {
            dataBlock = new ActionBlock<ISensorsData>(v => { Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { SensorsDataText = v.ToString(); }).AsTask().Wait(); });

            this.InitializeComponent();
        }

        private ActionBlock<ISensorsData> dataBlock;

        public ActionBlock<ISensorsData> SensorsDataBlock { get { return dataBlock; } }

        public string SensorsDataText { get { return dataLabel.Text; }  private set { dataLabel.Text = value; } }
    }
}
