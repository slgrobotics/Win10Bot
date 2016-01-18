using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks.Dataflow;

using slg.RobotBase.Interfaces;
using Windows.UI.Core;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace slg.Display
{
    public sealed partial class RobotStateDashboard : UserControl
    {
        private ActionBlock<IRobotState> dataBlockState;
        private ActionBlock<IRobotPose> dataBlockPose;

        public ActionBlock<IRobotState> StateDataBlock { get { return dataBlockState; } }
        public ActionBlock<IRobotPose> PoseDataBlock { get { return dataBlockPose; } }

        public string StateDataText { get { return dataLabelState.Text; } private set { dataLabelState.Text = value; } }

        public string PoseDataText { get { return dataLabelPose.Text; } private set { dataLabelPose.Text = value; } }

        public RobotStateDashboard()
        {
            dataBlockState = new ActionBlock<IRobotState>(v => { Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { StateDataText = v.ToString(); }).AsTask().Wait(); });
            dataBlockPose = new ActionBlock<IRobotPose>(v => { Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { PoseDataText = v.ToString(); }).AsTask().Wait(); });

            this.InitializeComponent();
        }
    }
}
