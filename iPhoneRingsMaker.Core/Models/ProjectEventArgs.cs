using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iPhoneRingsMaker.Core.Models;

public class ProjectEventArgs : EventArgs
{
    public ProjectEventArgs(M4RProject project)
    {
        Project = project;
    }
    public M4RProject Project
    {
        get;
    }
}
