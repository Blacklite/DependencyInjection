using Microsoft.Framework.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ClassLibrary1
{
    public class Class1
    {
        public Class1(IServiceCollection services)
        {
            services.AddAssembly(this);
        }
    }
}
