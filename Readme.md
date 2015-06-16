Dependency Injection Extras
===========================
[![Build status](https://ci.appveyor.com/api/projects/status/lm239yw7f0pkhchn?svg=true)](https://ci.appveyor.com/project/david-driscoll/dependencyinjection)  [![Build status](https://ci.appveyor.com/api/projects/status/lm239yw7f0pkhchn/branch/master?svg=true)](https://ci.appveyor.com/project/david-driscoll/dependencyinjection/branch/master)

Adds a few extra pieces of the dependency injection system built into [AspNet 5](http://github.com/aspnet/DependencyInection).


``[ServiceDescriptor]`` Attribute
-----------------------------
Adds an attribute to the `Microsoft.Framework.DependencyInjection` namespace, that allows you register a specific class with the DI system.

Used by itself the attribute will register all the classes (and base class) with the implementation.  You can specific a service type, to indicate a specific interface.  You can also indicate the lifetime as supported by AspNet 5.


`IServiceCollection.AddAssembly(....)`
--------------------------------------
Extension Method. Scans the given assembly, for the ServiceDescriptor attribute, and adds them to the Service Collection.

`ServiceDescriptor.FromAssembly(....)`
-------------------------------------
Extension Method. Scans the given assembly and generates descriptions for each one that was found.



Future Plans
============
With the new meta programming model being introduced in AspNet 5, there are plans (and samples) that use Roslyn to replace the `IServiceCollection.AddAssembly()` or `` calls with the appropriate services as defined in the assembly at runtime.

