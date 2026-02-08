using System;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.ProjectData;

public sealed record AdditionalInfoCacheKey(
    string ProjectPath,
    DateTime ProjectLastWriteUtc,
    string? AdditionalInfoPath,
    DateTime? AdditionalInfoLastWriteUtc);

public sealed class AdditionalInfoCache
{
    private AdditionalInfoCacheKey? _key;
    private AdditionalData? _data;

    public AdditionalData GetOrLoad(AdditionalInfoCacheKey key, Func<AdditionalData> loader)
    {
        if (loader is null)
        {
            throw new ArgumentNullException(nameof(loader));
        }

        if (_key is not null && _data is not null && _key.Equals(key))
        {
            return _data;
        }

        var data = loader();
        _key = key;
        _data = data;
        return data;
    }
}
