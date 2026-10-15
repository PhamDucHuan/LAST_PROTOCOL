using System;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace LastProtocol
{
    // Uses Relay APIs shipped INSIDE com.unity.services.multiplayer, not standalone Relay.
    public sealed class RelayConnectionService
    {
        private Task initialization;
        public Task InitializeAsync(string environment)
        {
            if (initialization == null || initialization.IsFaulted || initialization.IsCanceled)
                initialization = InitializeInternalAsync(environment);
            return initialization;
        }

        private static async Task InitializeInternalAsync(string environment)
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                var options = new InitializationOptions();
                if (!string.IsNullOrWhiteSpace(environment)) options.SetEnvironmentName(environment);
                // Persistent anonymous identity on each PC. Optional profile for multiple local builds.
                string[] args = Environment.GetCommandLineArgs();
                int profileIndex = Array.IndexOf(args, "--lp-auth-profile");
                string profile = profileIndex >= 0 && profileIndex + 1 < args.Length ? args[profileIndex + 1] : "lastprotocol";
                options.SetProfile(profile);
                await UnityServices.InitializeAsync(options);
            }
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        public async Task<RelayHostAllocation> AllocateHostAsync()
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MultiplayerConfig.MaxPlayers - 1);
            string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            return new RelayHostAllocation(allocation, code);
        }

        public Task<JoinAllocation> AllocateClientAsync(string code) => RelayService.Instance.JoinAllocationAsync(code);

        public static void Configure(UnityTransport transport, Allocation allocation) =>
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
        public static void Configure(UnityTransport transport, JoinAllocation allocation) =>
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
    }

    public readonly struct RelayHostAllocation
    {
        public readonly Allocation Allocation;
        public readonly string JoinCode;
        public RelayHostAllocation(Allocation allocation, string joinCode) { Allocation = allocation; JoinCode = joinCode; }
    }
}
