using System;
using System.Windows;
using System.Windows.Controls;


namespace MediaPlayerTests
{
    public class MediaElementWrapper
    {
        private readonly MediaElement _MediaElement;

        private enum Phase
        {
            Idle, // Media element has not opened any file
            Opening, // Media element is in the process of opening a file
            Opened, // Media element has successfully opened a file
            Closed // Media element is closed.  Unable to use or revive the wrapped media element from this point.
        }

        private bool _PlayRequest;
        private bool _ShutdownRequest;
        private Uri _OpenRequest;

        private Phase _Phase;
        
        public MediaElementWrapper()
            : this(new MediaElement())
        {}
        
        public MediaElementWrapper(MediaElement mediaElement)
        {
            _Phase = Phase.Idle;

            _MediaElement = mediaElement;

            _MediaElement.LoadedBehavior = MediaState.Manual;
            _MediaElement.UnloadedBehavior = MediaState.Manual;

            _MediaElement.MediaOpened += MediaPlayer_Opened;
            _MediaElement.MediaEnded += MediaPlayer_Ended;
            _MediaElement.MediaFailed += MediaPlayer_Failed;
        }

        public bool Repeat { get; set; }

        public void Open(Uri uri)
        {
            if (_Phase != Phase.Opening)
            {
                _OpenRequest = uri;
            }
            else if (_Phase != Phase.Closed)
            {
                _Phase = Phase.Opening;

                _MediaElement.Source = uri;
            }
        }

        public void Close()
        {
            _ShutdownRequest = true;

            if (_Phase != Phase.Opening && 
                _Phase != Phase.Closed)
            {
                Shutdown();
            }
        }

        public void Play()
        {
            if (_Phase == Phase.Opened)
            {
                _MediaElement.Play();
            }
            else
            {
                _PlayRequest = true;
            }
        }

        private void Shutdown()
        {
            _ShutdownRequest = false;

            _MediaElement.Stop();
            _MediaElement.Close();
            _MediaElement.Source = null;

            _MediaElement.MediaOpened -= MediaPlayer_Opened;
            _MediaElement.MediaEnded -= MediaPlayer_Ended;
            _MediaElement.MediaFailed -= MediaPlayer_Failed;

            _Phase = Phase.Closed;
        }

        private void MediaPlayer_Opened(object sender, RoutedEventArgs args)
        {
            _Phase = Phase.Opened;

            if (_ShutdownRequest)
            {
                Shutdown();
            }
            else
            {
                if (MediaOpened != null)
                    MediaOpened.Invoke(sender, args);

                if (_OpenRequest != null)
                {
                    Uri uri = _OpenRequest;
                    _OpenRequest = null; // Null thee request before using it so that unexpected things will not happen

                    // Feed the request back into the Open() function
                    Open(uri); 
                }
                else if (_PlayRequest)
                {
                    _PlayRequest = false;
                    _MediaElement.Play();
                }
            }
        }
        
        private void MediaPlayer_Ended(object sender, RoutedEventArgs args)
        {
            if (MediaEnded != null)
                MediaEnded.Invoke(sender, args);

            if (Repeat)
            {
                if (_Phase == Phase.Opened)
                {
                    _MediaElement.Position = TimeSpan.Zero;
                }
            }
        }

        private void MediaPlayer_Failed(object sender, RoutedEventArgs args)
        {
            Shutdown();

            if (MediaFailed != null)
                MediaFailed.Invoke(sender, args);
        }

        public event RoutedEventHandler MediaOpened;
        public event RoutedEventHandler MediaEnded;
        public event RoutedEventHandler MediaFailed;

        public bool IsMuted
        {
            get { return _MediaElement.IsMuted; }
            set { _MediaElement.IsMuted = value; }
        }

        public double Width
        {
            get { return _MediaElement.Width; }
            set { _MediaElement.Width = value; }
        }

        public double Height
        {
            get { return _MediaElement.Height; }
            set { _MediaElement.Height = value; }
        }
    }
}
