﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using xunit.runner.data;
using xunit.runner.wpf.ViewModel;

namespace xunit.runner.wpf.Impl
{
    internal partial class RemoteTestUtil 
    {
        private sealed class BackgroundRunner
        {
            private readonly ConcurrentQueue<TestResultData> _resultQueue;
            private readonly BinaryReader _reader;
            private readonly CancellationToken _cancellationToken;

            internal BackgroundRunner(ConcurrentQueue<TestResultData> resultQueue, BinaryReader reader, CancellationToken cancellationToken)
            {
                _resultQueue = resultQueue;
                _reader = reader;
                _cancellationToken = cancellationToken;
            }

            /// <summary>
            /// This will be called on a background thread to read the results of the test from the 
            /// named pipe client stream.
            /// </summary>
            /// <returns></returns>
            internal Task GoOnBackground()
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    TestResultData result;
                    try
                    {
                        result = TestResultData.ReadFrom(_reader);
                    }
                    catch
                    {
                        // Hacky way of detecting the stream being closed
                        break;
                    }

                    _resultQueue.Enqueue(result);
                }

                // Signal we are done 
                _resultQueue.Enqueue(null);

                return Task.FromResult(true);
            }
        }
    }
}
