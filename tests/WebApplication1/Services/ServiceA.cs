using System;
using Microsoft.Framework.DependencyInjection;

namespace WebApplication1.Services
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
