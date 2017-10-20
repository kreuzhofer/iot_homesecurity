//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq.Expressions;
//using System.Reactive.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Windows.Storage;
//using NLog;
//using NLog.Common;
//using NLog.Targets;

//namespace W10Home.App.Shared.Logging
//{
//    [Target("CustomFile")]
//    internal class CustomFileTarget : TargetWithLayoutHeaderAndFooter
//    {
//        private ConcurrentQueue<string> _lines = new ConcurrentQueue<string>();
//        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
//        private Task _backgroundTask;

//        public CustomFileTarget()
//        {
//        }

//        public CustomFileTarget(string name) : base()
//        {
//            this.Name = name;
//        }

//        protected override void Write(LogEventInfo logEvent)
//        {
//            _lines.Enqueue(this.RenderLogEvent(this.Layout, logEvent));
//        }

//        protected override void InitializeTarget()
//        {
//            _backgroundTask = Task.Factory.StartNew(Action, _cancellationTokenSource.Token);
//            base.InitializeTarget();
//        }

//        private async void Action(object token)
//        {
//            CancellationToken localToken = (CancellationToken) token;
//            do
//            {
//                if (_lines.TryDequeue(out string line))
//                {
//                    try
//                    {
//                        var fileDate = DateTime.Today.ToString("yyyy-MM-dd");
//                        var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileDate + ".log", CreationCollisionOption.OpenIfExists);
//                        await FileIO.AppendLinesAsync(file, new[] { line });
//                        await Task.Delay(1, localToken);
//                    }
//                    catch
//                    {
//                        // gulp
//                    }
//                }
//            } while (!localToken.IsCancellationRequested);
//        }

//        protected override void CloseTarget()
//        {
//            if (_backgroundTask.IsFaulted)
//            {
//                var ex = _backgroundTask.Exception; // eat up exception
//            }
//            _cancellationTokenSource.Cancel();
//            base.CloseTarget();
//        }
//    }
//}