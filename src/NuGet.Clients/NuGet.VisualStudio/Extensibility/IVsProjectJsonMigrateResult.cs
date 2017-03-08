using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.VisualStudio
{
    /// <summary>
    /// Contains the result of the migrate operation on a UWP project
    /// </summary>
    public interface IVsProjectJsonMigrateResult
    {
        /// <summary>
        /// Returns the success value of the migration operation.
        /// </summary>
        bool IsSuccess { get; }

        /// <summary>
        /// If migrate operation was successful, stores the path to the backup project file on disk.
        /// </summary>
        string BackupProjectFile { get; }

        /// <summary>
        /// If migrate operation was successful, stores the path to the backup project.json file on disk.
        /// </summary>
        string BackupProjectJsonFile { get; }
    }
}
