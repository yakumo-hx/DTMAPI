using System;

namespace RedSaw.CommandLineInterface;

public delegate bool QueryVariableType(string name, out Type type);
