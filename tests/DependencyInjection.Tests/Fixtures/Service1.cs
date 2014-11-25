﻿using Blacklite.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection;
using System;

namespace DependencyInjection.Tests.Fixtures
{
    public interface IService1
    {
        string Value { get; }
    }

    [ServiceDescriptor(typeof(IService1))]
    public class Service1 : IService1
    {
        public string Value
        {
            get
            {
                return "OVER 9000";
            }
        }
    }
}
