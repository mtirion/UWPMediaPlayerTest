using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UWPMediaPlayerTest
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            MediaManager.Player = LeftMedia.MediaPlayer;
            //RightMedia.SetMediaPlayer(LeftMedia.MediaPlayer);
        }

        private async void Load_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".mp4");
            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
                LeftMedia.MediaPlayer.SetFileSource(file);
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            LeftMedia.MediaPlayer.Play();
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            LeftMedia.MediaPlayer.Pause();
        }

        private async void Folder_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker picker = new FolderPicker();
            picker.FileTypeFilter.Add("*");
            StorageFolder folder = await picker.PickSingleFolderAsync();

            if (folder != null)
            {
                Stopwatch sw = Stopwatch.StartNew();
                QueryOptions options = new QueryOptions();
                options.FolderDepth = FolderDepth.Deep;
                var result = folder.CreateItemQueryWithOptions(options);
                var items = await result.GetItemsAsync();
                sw.Stop();
                uint count = await result.GetItemCountAsync();
                int pos = folder.Path.Length + 1;
                foreach (var item in items)
                {
                    if (item.IsOfType(StorageItemTypes.File))
                        Debug.WriteLine(item.Path.Substring(pos));
                }
                Debug.WriteLine($"DONE in {sw.ElapsedMilliseconds} ms for {count} files");
                Debugger.Break();
            }
        }

        #region Projection 
        public async Task StartProjecting()
        {
            try
            {
                if (Window.Current == null)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        internalStartProjecting();
                    });
                }
                else
                {
                    internalStartProjecting();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"*** ERROR Start Projecting: {ex.Message}");
            }
        }

        private bool projecting = false;
        private int mainViewId = -1;
        private int externalViewId = -1;
        private CoreDispatcher mainDispatcher;

        private async void internalStartProjecting()
        {
            try
            {
                // If projection is already in progress, then it could be shown on the monitor again
                // Otherwise, we need to create a new view to show the presentation
                if (projecting == false && externalViewId == -1) // && Window.Current != null)
                {
                    mainViewId = ApplicationView.GetForCurrentView().Id;
                    // First, create a new, blank view
                    var thisDispatcher = Window.Current.Dispatcher;
                    await CoreApplication.CreateNewView().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        // Assemble some data necessary for the new page
                        mainDispatcher = thisDispatcher;
                        externalViewId = ApplicationView.GetForCurrentView().Id;

                        // Display the page in the view. Note that the view will not become visible
                        // until "StartProjectingAsync" is called
                        var rootFrame = new Frame();
                        rootFrame.Navigate(typeof(ProjectionPage));
                        Window.Current.Content = rootFrame;

                        // The call to Window.Current.Activate is required starting in Windos 10.
                        // Without it, the view will never appear.
                        Window.Current.Activate();
                    });
                }

                projecting = true;

                // Start/StopViewInUse are used to signal that the app is interacting with the
                // view, so it shouldn't be closed yet, even if the user loses access to it
                if (projecting)
                {
                    // Show the view on a second display (if available) or on the primary display
                    await ProjectionManager.StartProjectingAsync(externalViewId, mainViewId);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("StartProjection: " + ex.Message);
            }
        }

        public async Task StopProjecting()
        {
            try
            {
                if (Window.Current == null)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        internalStopProjecting();
                    });
                }
                else
                {
                    internalStopProjecting();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"*** ERROR StopProjecting: {ex.Message}");
            }
        }

        private async void internalStopProjecting()
        {
            try
            {
                if (projecting)
                {
                    // There may be cases to end the projection from the projected view
                    // (e.g. the presentation hosted in that view concludes)
                    await ProjectionManager.StopProjectingAsync(externalViewId, ApplicationView.GetForCurrentView().Id);
                    projecting = false;
                }
            }
            catch (Exception ex)
            {   // this occurs when external screen was shut off - view id does not exist anymore.
                Debug.WriteLine("StopProjection: " + ex.Message);
            }
        }

        public async Task SwapProjection()
        {
            try
            {
                if (projecting)
                {
                    await ProjectionManager.SwapDisplaysForViewsAsync(externalViewId, ApplicationView.GetForCurrentView().Id);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SwapProjection: " + ex.Message);
            }
        }
        #endregion

        private void Projection_Click(object sender, RoutedEventArgs e)
        {
            if (!projecting)
            {
                StartProjecting();
            }
            else
            {
                StopProjecting();
            }
        }
    }
}
