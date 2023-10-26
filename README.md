# NET 7.0 Pluggable Agent

A **pluggable agent** for the TestCentric Gui Runner, capable of running tests under .NET 7.0.

## About Pluggable Agents

Version 2 of the **TestCentric Engine** will no longer have built-in agents for running tests. All agents will
be distributed separately, as **pluggable agents**, which are a particulary type of engine extension supported
by TestCentric.

Of course, a runner without any agents would not be capable of doing anything useful. So, each runner we provide
will come with at least one **pluggable agent** pre-installed. For example the  2.0 release of the GUI will
include agents targeting `net462`, `net6.0` and `net7.0`. 

The **pluggable agents** provided with a runner are still separate packages, but are listed as dependencies of
the runner package itself. This allows us to modify easily modify the set of standard agents provided for a
runner over it's life cycle and over the life cycle of the supported .NET platforms.

## Notes

1. All **pluggable agents** are being released with an initial version of 2.0.0. This is done to serve as a
reminder that the agents will only function in version 2.0.0 or higher of the TestCentric Gui Runner.

2. The package id for each **pluggable agent** uses the NUnit naming convention: _NUnit.Extension.*_ or
_nunit-extension-*_. However, no releases of NUnit 3 currently support this type of extension and it's not
clear whether they will be supported in future releases.
