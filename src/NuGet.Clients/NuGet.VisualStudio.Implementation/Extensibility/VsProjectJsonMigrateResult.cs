using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.VisualStudio.Implementation
{
    class VsProjectJsonMigrateResult : IVsProjectJsonMigrateResult
    {
        public bool IsSuccess => throw new NotImplementedException();

        public string BackupProjectFile => throw new NotImplementedException();

        public string BackupProjectJsonFile => throw new NotImplementedException();
    }
}
