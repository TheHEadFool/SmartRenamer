using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Scout.Observations.Conversation;

namespace SmartRenamer.Observations.Experts.EbookExpert.Action
{
    /// <summary>
    /// Runs external Ebook Expert research away from the synchronous
    /// investigation path.
    ///
    /// The coordinator intentionally uses one worker. This keeps Scout's
    /// processing responsive without creating an uncontrolled burst of
    /// requests against an external research provider.
    /// </summary>
    internal sealed class E_ExternalResearchCoordinator
    {
        private sealed class WorkItem
        {
            public string Key { get; init; } = string.Empty;

            public Func<CancellationToken, Task<CV_ActionResult>> Work { get; init; } =
                _ => Task.FromResult(new CV_ActionResult());

            public Action<CV_ActionResult> Completed { get; init; } =
                _ => { };

            public long Generation { get; init; }
        }

        private readonly ConcurrentQueue<WorkItem> _queue = new();

        private readonly SemaphoreSlim _signal = new(0);

        private readonly CancellationTokenSource _cancellation = new();

        private readonly object _keysLock = new();

        private readonly HashSet<string> _queuedOrRunningKeys =
            new(StringComparer.OrdinalIgnoreCase);

        private long _generation;

        public E_ExternalResearchCoordinator()
        {
            _ = Task.Run(ProcessQueueAsync);
        }

        public bool Enqueue(
            string key,
            Func<CancellationToken, Task<CV_ActionResult>> work,
            Action<CV_ActionResult> completed)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(
                    "Background work requires a stable key.",
                    nameof(key));

            ArgumentNullException.ThrowIfNull(work);
            ArgumentNullException.ThrowIfNull(completed);

            long generation;

            lock (_keysLock)
            {
                if (!_queuedOrRunningKeys.Add(key))
                    return false;

                generation = _generation;
            }

            _queue.Enqueue(
                new WorkItem
                {
                    Key = key,
                    Work = work,
                    Completed = completed,
                    Generation = generation
                });

            _signal.Release();
            return true;
        }

        public void Reset()
        {
            while (_queue.TryDequeue(out WorkItem? item))
            {
                lock (_keysLock)
                {
                    _queuedOrRunningKeys.Remove(item.Key);
                }
            }

            // Running work is deliberately not force-aborted here. The
            // current ISBN resource owns its provider timeout. A generation
            // change makes its eventual completion stale, so it cannot alter
            // the new expedition.
            lock (_keysLock)
            {
                _generation++;
                _queuedOrRunningKeys.Clear();
            }
        }

        private async Task ProcessQueueAsync()
        {
            while (!_cancellation.IsCancellationRequested)
            {
                try
                {
                    await _signal.WaitAsync(
                        _cancellation.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                while (_queue.TryDequeue(out WorkItem? item))
                {
                    CV_ActionResult result;

                    try
                    {
                        result = await item.Work(
                            _cancellation.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        result = new CV_ActionResult
                        {
                            ActionId = "BackgroundResearchCancelled",
                            Success = false,
                            Message =
                                "The external research operation was cancelled before it could finish."
                        };
                    }
                    catch (Exception ex)
                    {
                        result = new CV_ActionResult
                        {
                            ActionId = "BackgroundResearchFailed",
                            Success = false,
                            Message =
                                "The external research operation failed safely: " +
                                ex.Message
                        };
                    }
                    finally
                    {
                        lock (_keysLock)
                        {
                            if (item.Generation == _generation)
                            {
                                _queuedOrRunningKeys.Remove(item.Key);
                            }
                        }
                    }

                    bool currentGeneration;

                    lock (_keysLock)
                    {
                        currentGeneration =
                            item.Generation == _generation;
                    }

                    if (currentGeneration)
                    {
                        try
                        {
                            item.Completed(result);
                        }
                        catch
                    {
                            // Background completion must never terminate the
                            // research worker. The workflow owns presentation and
                            // error handling for completion callbacks.
                        }
                    }

                    // Keep external research deliberately serialized. The
                    // provider-specific resource remains responsible for its
                    // own timeout and request behavior.
                    try
                    {
                        await Task.Delay(
                            TimeSpan.FromSeconds(1),
                            _cancellation.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }
        }
    }
}
