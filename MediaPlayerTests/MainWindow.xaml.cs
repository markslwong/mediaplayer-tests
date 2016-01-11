using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using JetBrains.Profiler.Windows.Api;


namespace MediaPlayerTests
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int MaxVideos = 3;
        private const int SwitchTimeMilliseconds = 1000;
        private const int DumpEverySwitchCount = int.MaxValue;

        private readonly List<StackPanel> _cells = new List<StackPanel>();
        private readonly MediaElementWrapper[] _wrappers;
        private readonly DispatcherTimer _timer;
        
        private readonly string[] _files;
        private readonly long[] _sizes;

        private int _indexCell = -MaxVideos;
        private int _indexFile;

        private int _numSwitches;
        
        public MainWindow()
        {
            InitializeComponent();
            
            _cells.Add(Cell00);
            _cells.Add(Cell01);
            _cells.Add(Cell02);
            _cells.Add(Cell03);
            _cells.Add(Cell04);
            _cells.Add(Cell10);
            _cells.Add(Cell11);
            _cells.Add(Cell12);
            _cells.Add(Cell13);
            _cells.Add(Cell14);
            _cells.Add(Cell20);
            _cells.Add(Cell21);
            _cells.Add(Cell22);
            _cells.Add(Cell23);
            _cells.Add(Cell24);
            _cells.Add(Cell30);
            _cells.Add(Cell31);
            _cells.Add(Cell32);
            _cells.Add(Cell33);
            _cells.Add(Cell34);
            _cells.Add(Cell40);
            _cells.Add(Cell41);
            _cells.Add(Cell42);
            _cells.Add(Cell43);
            _cells.Add(Cell44);

            _wrappers = new MediaElementWrapper[_cells.Count];

            _files = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Videos\\IliaSequencer");
            _sizes = new long[_files.Length];

            for (var i = 0; i < _files.Length; ++i)
            {
                var fileInfo = new FileInfo(_files[i]);
                _sizes[i] = fileInfo.Length;
            }
            
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(SwitchTimeMilliseconds)
            };

            _timer.Tick += TimerOnElapsed;

            if (MemoryProfiler.IsActive && MemoryProfiler.CanControlAllocations)
                MemoryProfiler.EnableAllocations();

            _timer.Start();

            TimerOnElapsed(this, EventArgs.Empty);
        }

        private void TimerOnElapsed(object sender, EventArgs args)
        {
            if (MemoryProfiler.IsActive)
            {
                if (_numSwitches % DumpEverySwitchCount == 0)
                {
                    MemoryProfiler.Dump();
                }
            }

            _numSwitches++;

            for (int i = 0; i < MaxVideos; ++i)
            {
                var indexCell = (_indexCell + i) % _cells.Count;
                var indexFile = (_indexFile + i) % _files.Length;

                StopVideo(indexCell, indexFile);
            }

            _indexCell = (_indexCell + MaxVideos) % _cells.Count;
            _indexFile = (_indexFile + MaxVideos) % _files.Length;

            for (int i = 0; i < MaxVideos; ++i)
            {
                var indexCell = (_indexCell + i) % _cells.Count;
                var indexFile = (_indexFile + i) % _files.Length;

                PlayVideo(indexCell, indexFile);
            }
        }

        private void PlayVideo(int indexCell, int indexFile)
        {
            MediaElementWrapper wrapper = new MediaElementWrapper
            {
                Repeat = true,
                IsMuted = true
            };

            wrapper.Subject.Visibility = Visibility.Visible;

            wrapper.Open(new Uri(_files[indexFile]));

            _wrappers[indexCell] = wrapper;

            _cells[indexCell].Children.Clear();
            _cells[indexCell].Children.Add(wrapper.Subject);

            Console.WriteLine("Now playing {0} on cell {1}.", _files[indexFile], indexCell);
        }

        private void StopVideo(int indexCell, int indexFile)
        {
            if (indexCell < 0)
                return;

            if (_wrappers[indexCell] != null)
            {
                _wrappers[indexCell].Close();
                _wrappers[indexCell] = null;

                _cells[indexCell].Children.Clear();
            }
        }
    }
}
