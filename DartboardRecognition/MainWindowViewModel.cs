﻿#region Usings

using System.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

#endregion

namespace DartboardRecognition
{
    public class MainWindowViewModel
    {
        private MainWindow mainWindowView;
        private Dispatcher mainWindowDispatcher;
        private Drawman drawman;
        private ThrowService throwService;
        private CancellationToken cancelToken;
        private CancellationTokenSource cts;

        public MainWindowViewModel()
        {
        }

        public MainWindowViewModel(MainWindow mainWindowView)
        {
            this.mainWindowView = mainWindowView;
            mainWindowDispatcher = mainWindowView.Dispatcher;
        }

        private void StartCapturing()
        {
            drawman = new Drawman();
            cts = new CancellationTokenSource();
            cancelToken = cts.Token;
            throwService = new ThrowService(mainWindowView, drawman);

            var dartboardProjectionImage = throwService.PrepareDartboardProjectionImage();
            mainWindowView.DartboardProjectionImageBox.Source = drawman.ConvertToBitmap(dartboardProjectionImage);

            var runtimeCapturing = mainWindowView.RuntimeCapturingCheckBox.IsChecked.Value;

            StartCam(1, runtimeCapturing);
            StartCam(2, runtimeCapturing);
            StartThrowService();
        }

        private void StopCapturing()
        {
            cts.Cancel();
            mainWindowView.DartboardProjectionImageBox.Source = new BitmapImage();
        }

        private void StartThrowService()
        {
            var thread = new Thread(() =>
                                    {
                                        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));

                                        throwService.AwaitForThrow(cancelToken);

                                        Dispatcher.Run();
                                    });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void StartCam(int camNumber, bool runtimeCapturing)
        {
            var thread = new Thread(() =>
                                    {
                                        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));

                                        var camWindow = new CamWindow(camNumber, drawman, throwService, cancelToken);
                                        camWindow.Closed += (s, args) =>
                                                                Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                                        camWindow.Run(runtimeCapturing);

                                        Dispatcher.Run();
                                    });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        public void OnStartButtonClicked()
        {
            ToggleViewControls();
            StartCapturing();
        }

        public void OnStopButtonClicked()
        {
            mainWindowView.PointsBox.Text = "";
            ToggleViewControls();
            StopCapturing();
        }

        private void ToggleViewControls()
        {
            mainWindowView.StartButton.IsEnabled = !mainWindowView.StartButton.IsEnabled;
            mainWindowView.StopButton.IsEnabled = !mainWindowView.StopButton.IsEnabled;
        }
    }
}