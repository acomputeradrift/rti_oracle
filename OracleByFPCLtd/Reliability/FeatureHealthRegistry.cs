using System;
using System.Collections.Generic;

namespace OracleByFPCLtd.Reliability;

public sealed class FeatureHealthRegistry
{
    private readonly Dictionary<string, FeatureOperation> _latestByKey = new(StringComparer.OrdinalIgnoreCase);

    public void Update(FeatureOperation operation)
    {
        var key = BuildKey(operation.Feature, operation.Target);
        _latestByKey[key] = operation;
    }

    public bool TryGet(string feature, string target, out FeatureOperation? operation)
    {
        var key = BuildKey(feature, target);
        var found = _latestByKey.TryGetValue(key, out var existing);
        operation = existing;
        return found;
    }

    private static string BuildKey(string feature, string target)
    {
        return $"{feature}::{target}";
    }
}
