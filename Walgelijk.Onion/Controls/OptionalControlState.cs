﻿using System.Diagnostics.CodeAnalysis;

namespace Walgelijk.Onion.Controls;

public class OptionalControlState<T>
{
    public record State(T Value, bool IncomingChange);
    public static readonly Dictionary<int, State> ByIdentity = new();

    public void SetValue(int identity, T value)
    {
        ByIdentity.AddOrSet(identity, new State(value, true));
    }

    public T GetValue(int identity)
    {
        if (ByIdentity.TryGetValue(identity, out var state))
            return state.Value;
        throw new Exception($"Identity {identity} asked for a state it does not have");
    }

    public bool HasValue(int identity) => ByIdentity.ContainsKey(identity);

    public bool TryGetState(int identity, [NotNullWhen(true)] out T? val)
    {
        if (ByIdentity.TryGetValue(identity, out var state))
        {
            val = state.Value;
            return true;
        }
        val = default;
        return false;
    }

    public void UpdateFor(int identity, ref T v)
    {
        if (ByIdentity.TryGetValue(identity, out var state))
        {
            if (state.IncomingChange)
                v = state.Value;
            ByIdentity[identity] = new(v, false);
        }
        else
            ByIdentity.Add(identity, new(v, false));
    }

    public bool HasIncomingChange(int identity) => ByIdentity.TryGetValue(identity, out var state) && state.IncomingChange;

    public T this[int identity]
    {
        get => GetValue(identity);
        set => SetValue(identity, value);
    }
}