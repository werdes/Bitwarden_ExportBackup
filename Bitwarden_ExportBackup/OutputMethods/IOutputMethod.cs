using Bitwarden_ExportBackup.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bitwarden_ExportBackup.OutputMethod
{
    public interface IOutputMethod
    {
        bool Archive(List<ExportItem> exportItems);
        string GetExtension();
    }
}
