using System;
using Microsoft.Framework.DependencyInjection;

namespace ClassLibrary1.Services
{
    [ServiceDescriptor]
    public class ServiceA
    {

    }


    public interface IServiceB
    {

    }

    [ServiceDescriptor(typeof(IServiceB))]
    class ServiceB : IServiceB
    {

    }
}
