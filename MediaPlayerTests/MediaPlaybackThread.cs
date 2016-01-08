using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;


namespace MediaPlayerTests
{
    public class MediaPlaybackThread : IDisposable
    {
        private readonly HostVisual _HostVisual;
        private readonly Thread _Thread;
        
        private VisualTarget _VisualTarget;
        private MediaElement _MediaElement;
        
        private VisualTargetPresentationSource _VisualTargetSource;

        private volatile bool _CanPlay;
        private PlaybackInfo _NextToPlay;

        private class PlaybackInfo
        {
            public string FileName { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
        }

        public MediaPlaybackThread(HostVisual hostVisual)
        {
            _HostVisual = hostVisual;
            _CanPlay = true;
            
            _Thread = new Thread(Run)
            {
                IsBackground = true
            };

            _Thread.SetApartmentState(ApartmentState.STA);
            _Thread.Start();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_Thread)
                {
                    Dispatcher dispatcher = Dispatcher.FromThread(_Thread);
                    if (dispatcher != null)
                        dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
                    else
                        _Thread.Abort();
                }
            }
        }

        private void MediaPlayer_Opened(object sender, RoutedEventArgs args)
        {
            _CanPlay = true;

            lock (_Thread)
            {
                if (_NextToPlay != null)
                {
                    DestroyMediaPlayer();
                    CreateMediaPlayer();
                }
            }
        }

        private void MediaPlayer_Ended(object sender, RoutedEventArgs args)
        {
            lock (_Thread)
            {
                MediaElement mediaElement = sender as MediaElement;
                Debug.Assert(mediaElement != null);
                mediaElement.Position = TimeSpan.Zero; // Repeats the video
            }
        }

        private void MediaPlayer_Failed(object sender, RoutedEventArgs arg)
        {
            lock (_Thread)
            {
                DestroyMediaPlayer();

                if (_CanPlay && _NextToPlay != null)
                {
                    CreateMediaPlayer();
                }
            }
        }
        
        public void Play(string fileName, double width, double height)
        {
            Dispatcher dispatcher;

            lock (_Thread)
            {
                _NextToPlay = new PlaybackInfo
                {
                    FileName = fileName,
                    Width = width,
                    Height = height
                };

                dispatcher = Dispatcher.FromThread(_Thread);

                if (dispatcher == null)
                    return;
            }

            dispatcher.InvokeAsync(() =>
            {
                lock (_Thread)
                {
                    if (_CanPlay)
                    {
                        DestroyMediaPlayer();
                        CreateMediaPlayer();
                    }
                }
            });
        }

        public void Stop()
        {
            Dispatcher dispatcher = Dispatcher.FromThread(_Thread);

            if (dispatcher != null)
            {
                dispatcher.InvokeAsync(() =>
                {
                    lock (_Thread)
                    {
                        if (_CanPlay)
                            DestroyMediaPlayer();
                    }
                });
            }
        }

        private void CreateMediaPlayer()
        {
            _MediaElement = new MediaElement
            {
                LoadedBehavior = MediaState.Manual,
                UnloadedBehavior = MediaState.Manual,
                IsMuted = true,
                IsHitTestVisible = false
            };

            _VisualTargetSource.RootVisual = _MediaElement;

            _MediaElement.MediaOpened += MediaPlayer_Opened;
            _MediaElement.MediaEnded += MediaPlayer_Ended;
            _MediaElement.MediaFailed += MediaPlayer_Failed;

            if (_CanPlay && _NextToPlay != null)
            {
                _CanPlay = false;
                
                _MediaElement.Source = new Uri(_NextToPlay.FileName);
                _MediaElement.Width = _NextToPlay.Width;
                _MediaElement.Height = _NextToPlay.Height;
                _MediaElement.Play();

                _NextToPlay = null;
            }
        }

        private void DestroyMediaPlayer()
        {
            if (_MediaElement != null)
            {
                _MediaElement.MediaOpened -= MediaPlayer_Opened;
                _MediaElement.MediaEnded -= MediaPlayer_Ended;
                _MediaElement.MediaFailed -= MediaPlayer_Failed;

                _MediaElement.Stop();
                _MediaElement.Close();
                
                _MediaElement = null;
            }

            _CanPlay = true;
            
            _VisualTargetSource.RootVisual = null;
        }

        private void Run()
        {
            try
            {
                _VisualTarget = new VisualTarget(_HostVisual);
                _VisualTargetSource = new VisualTargetPresentationSource(_VisualTarget);
                
                lock (_Thread)
                {
                    CreateMediaPlayer();
                }

                _VisualTargetSource.RootVisual = _MediaElement;

                Dispatcher.Run();
            }
            finally
            {
                if (_VisualTarget != null)
                {
                    _VisualTarget.Dispose();
                }

                lock (_Thread)
                {
                    DestroyMediaPlayer();
                }

                _CanPlay = false;
            }
        }

        private class VisualTargetPresentationSource : PresentationSource
        {
            private readonly VisualTarget _VisualTarget;

            public VisualTargetPresentationSource(VisualTarget visualTarget)
            {
                _VisualTarget = visualTarget;
            }

            protected override CompositionTarget GetCompositionTargetCore()
            {
                return _VisualTarget;
            }

            public override Visual RootVisual
            {
                get { return _VisualTarget.RootVisual; }
                set
                {
                    Visual oldRoot = _VisualTarget.RootVisual;

                    _VisualTarget.RootVisual = value;

                    RootChanged(oldRoot, value);

                    UIElement rootElement = value as UIElement;
                    if (rootElement != null)
                    {
                        rootElement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                        rootElement.Arrange(new Rect(rootElement.DesiredSize));
                    }
                }
            }

            public override bool IsDisposed
            {
                get { return false; }
            }
        }
    }
}
