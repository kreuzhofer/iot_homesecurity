using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTHs.Plugin.AzureIoTHub
{
    public static class ObservableExtensions
    {
        private static IObservable<T> BackOffAndRetry<T>(
            this IObservable<T> source,
            Func<int, TimeSpan> strategy,
            Func<int, Exception, bool> retryOnError,
            int attempt)
        {
            return Observable
                .Defer(() =>
                {
                    var delay = attempt == 0 ? TimeSpan.Zero : strategy(attempt);
                    var s = delay == TimeSpan.Zero ? source : source.DelaySubscription(delay);

                    return s
                        .Catch<T, Exception>(e =>
                        {
                            if (retryOnError(attempt, e))
                            {
                                return source.BackOffAndRetry(strategy, retryOnError, attempt + 1);
                            }
                            return Observable.Throw<T>(e);
                        });
                });
        }

        public static IObservable<T> BackOffAndRetry<T>(
            this IObservable<T> source,
            Func<int, TimeSpan> strategy,
            Func<int, Exception, bool> retryOnError)
        {
            return source.BackOffAndRetry(strategy, retryOnError, 0);
        }
    }
}
