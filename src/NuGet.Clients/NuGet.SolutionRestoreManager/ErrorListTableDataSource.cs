﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using NuGet.PackageManagement.UI;

namespace NuGet.SolutionRestoreManager
{
    /// <summary>
    /// Add/Remove warnings/errors from the error list.
    /// This persists messages once they are added.
    /// </summary>
    [Export(typeof(ErrorListTableDataSource))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class ErrorListTableDataSource : ITableDataSource
    {
        private readonly object _lockObj = new object();
        private readonly IServiceProvider _serviceProvider;
        private readonly List<TableSubscription> _subscriptions = new List<TableSubscription>();

        public string SourceTypeIdentifier => StandardTableDataSources.ErrorTableDataSource;

        public string Identifier => "NuGetRestoreManagerListTable";

        public string DisplayName => "NuGet_Restore_Manager_Table_Data_Source";

        public IErrorList _errorList;
        public ITableManager _tableManager;
        private bool _initialized;

        [ImportingConstructor]
        public ErrorListTableDataSource(
            [Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _serviceProvider = serviceProvider;
        }

        public IDisposable Subscribe(ITableDataSink sink)
        {
            var subscription = new TableSubscription(sink);

            lock (_subscriptions)
            {
                // Clean out disposed subscriptions
                _subscriptions.RemoveAll(e => e.Disposed);

                // Add the new subscription
                _subscriptions.Add(subscription);
            }

            return subscription;
        }

        /// <summary>
        /// Clear only nuget entries.
        /// </summary>
        public void ClearNuGetEntries()
        {
            EnsureInitialized();

            foreach (var subscription in GetSubscriptionsWithLock())
            {
                subscription.RunWithLock((sink) =>
                {
                    var entries = _errorList.TableControl.Entries.Where(IsNuGetEntry).ToArray();

                    sink.RemoveEntries(entries);
                });
            }
        }

        /// <summary>
        /// Add error list entries
        /// </summary>
        public void AddEntries(params ErrorListTableEntry[] entries)
        {
            if (entries.Length > 0)
            {
                EnsureInitialized();

                foreach (var subscription in GetSubscriptionsWithLock())
                {
                    subscription.RunWithLock((sink) =>
                    {
                        sink.AddEntries(entries.ToList(), removeAllEntries: false);
                    });
                }
            }
        }

        /// <summary>
        /// Show error window.
        /// </summary>
        public void BringToFront()
        {
            EnsureInitialized();

            // Give the error list focus.
            var vsErrorList = _errorList as IVsErrorList;
            vsErrorList?.BringToFront();
        }

        // Lock before calling
        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                // Double check around locking since this is called often.
                lock (_lockObj)
                {
                    if (!_initialized)
                    {
                        NuGetUIThreadHelper.JoinableTaskFactory.Run(async () =>
                        {
                            await NuGetUIThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                            // Get the error list service from the UI thread
                            _errorList = _serviceProvider.GetService(typeof(SVsErrorList)) as IErrorList;
                            _tableManager = _errorList.TableControl.Manager;

                            _tableManager.AddSource(this);
                            _initialized = true;
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Lock and enumerate subscriptions.
        /// </summary>
        private TableSubscription[] GetSubscriptionsWithLock()
        {
            lock (_subscriptions)
            {
                // Clean out disposed subscriptions
                _subscriptions.RemoveAll(e => e.Disposed);

                // Create a copy of the list to enumerate on
                return _subscriptions.ToArray();
            }
        }

        private static bool IsNuGetEntry(ITableEntryHandle entry)
        {
            object sourceObj;
            return (entry != null
                && entry.TryGetValue(StandardTableColumnDefinitions.ErrorSource, out sourceObj)
                && StringComparer.Ordinal.Equals(ErrorListTableEntry.ErrorSouce, (sourceObj as string)));
        }

        public void Dispose()
        {
            if (_initialized)
            {
                // This does not need to be on the UI thread.
                _tableManager.RemoveSource(this);
            }
        }

        /// <summary>
        /// Holds an ITableDataSink and lock.
        /// </summary>
        private class TableSubscription : IDisposable
        {
            private readonly object _lockObj = new object();
            private ITableDataSink _sink;

            public bool Disposed { get; private set; }

            public TableSubscription(ITableDataSink sink)
            {
                _sink = sink;
            }

            public void RunWithLock(Action<ITableDataSink> action)
            {
                lock (_lockObj)
                {
                    if (!Disposed)
                    {
                        Debug.Assert(_sink != null, "ITableDataSink null, unable to log warnings/errors");

                        if (_sink != null)
                        {
                            action(_sink);
                        }
                    }
                }
            }

            public void Dispose()
            {
                lock (_lockObj)
                {
                    _sink = null;
                    Disposed = true;
                }
            }
        }
    }
}
