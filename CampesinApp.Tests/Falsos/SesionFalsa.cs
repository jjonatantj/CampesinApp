using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace CampesinApp.Tests.Falsos;

/// <summary>
/// Sesión HTTP en memoria para probar controladores que leen HttpContext.Session.
/// </summary>
public class SesionFalsa : ISession
{
    private readonly Dictionary<string, byte[]> _datos = new();

    public string Id => "sesion-prueba";
    public bool IsAvailable => true;
    public IEnumerable<string> Keys => _datos.Keys;

    public void Clear() => _datos.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _datos.Remove(key);
    public void Set(string key, byte[] value) => _datos[key] = value;
    public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) => _datos.TryGetValue(key, out value);
}

