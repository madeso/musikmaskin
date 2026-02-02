using KdlSharp;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace MusikMaskin.KdlEval;

interface Argument
{
    string Parse(KdlValue value);
}

internal class ArgumentFunc(Func<KdlValue, string> func) : Argument
{
    public string Parse(KdlValue value)
    {
        return func(value);
    }
}

internal class Parser
{
    private List<(string, Argument)> named = new();
    private Dictionary<string, Argument> optional = new();

    private void Add(string name, Argument arg)
    {
        if (name[0] == '-')
        {
            optional.Add(name[1..], arg);
        }
        else
        {
            named.Add((name, arg));
        }
    }

    public void AddDouble(string name, Action<double> p1)
    {
        Add(name, new ArgumentFunc(v =>
        {
            var val = v.AsDouble();
            if (val == null) return "was not a double";
            p1(val.Value);
            return "";
        }));
    }

    public void AddString(string name, Action<string> s1)
    {
        Add(name, new ArgumentFunc(v =>
        {
            var val = v.AsString();
            if (val == null) return "was not a string";
            s1(val);
            return "";
        }));
    }

    public void AddStringWithError(string name, Func<string, string> s1)
    {
        Add(name, new ArgumentFunc(v =>
        {
            var val = v.AsString();
            if (val == null) return "was not a string";
            return s1(val);
        }));
    }

    public void AddInt(string name, Action<int> p1)
    {
        Add(name, new ArgumentFunc(v =>
        {
            var val = v.AsInt32();
            if (val == null) return "was not a int";
            p1(val.Value);
            return "";
        }));
    }

    public bool Parse(KdlNode node, Action<string> onError)
    {
        bool status = true;

        if (named.Count != node.Arguments.Count)
        {
            onError($"Different argument count. Expected {named.Count} but got {node.Arguments.Count}");
            status = false;
        }

        for (int i = 0; i < Math.Min(node.Arguments.Count, named.Count); i += 1)
        {
            var (name, arg) = named[i];
            var r = arg.Parse(node.Arguments[i]);
            if (r != "")
            {
                onError($"{name} argument at {i + 1}: {r}");
                status = false;
            }
        }

        foreach (var prop in node.Properties)
        {
            if (optional.TryGetValue(prop.Key, out var found) == false)
            {
                status = false;
                onError($"Unknown property: {prop.Key}");
                continue;
            }

            var r = found.Parse(prop.Value);
            if (r != "")
            {
                onError($"Property {prop.Key}: {r}");
                status = false;
            }
        }

        return status;
    }
}
