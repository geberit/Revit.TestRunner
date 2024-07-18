using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit.TestRunner.Shared
{
    public partial class _readme: Component
    {    
        public _readme()
        {
            InitializeComponent();
        }

        public _readme(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
        }
    }
}
