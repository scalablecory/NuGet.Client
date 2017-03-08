using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;

namespace NuGet.VisualStudio.Implementation
{
    class VsProjectJsonMigrator : IVsProjectJsonMigrator
    {
        public Task<IVsProjectJsonMigrateResult> MigrateProjectToPackageRef(Project project)
        {
            throw new NotImplementedException();
        }
    }
}
