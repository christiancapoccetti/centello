using System.Reflection;
using Centello.Application;
using Centello.Infrastructure;

namespace Centello.App;

internal static class InviteVerificationExtensions
{
    internal static IServiceCollection AddAiInviteVerification(this IServiceCollection services)
    {
        var publicKey = typeof(InviteVerificationExtensions).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .SingleOrDefault(attribute => attribute.Key == "CentelloInvitePublicKey")?.Value ?? "";
        services.AddSingleton<IAiInviteVerifier>(new AiInviteVerifier(publicKey));
        return services;
    }
}
