using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace MyFolder._1._Scripts._4._Network
{
	public static class RelayConnector
	{
		public static async Task EnsureUgsAsync()
		{
			if (UnityServices.State == ServicesInitializationState.Uninitialized)
				await UnityServices.InitializeAsync();
			if (!AuthenticationService.Instance.IsSignedIn)
				await AuthenticationService.Instance.SignInAnonymouslyAsync();
		}

		public static async Task<(Allocation alloc, string joinCode)> CreateHostAsync(int maxPlayers)
		{
			await EnsureUgsAsync();
			var alloc = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
			var code = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
			return (alloc, code);
		}

		public static async Task<JoinAllocation> JoinAsync(string joinCode)
		{
			await EnsureUgsAsync();
			return await RelayService.Instance.JoinAllocationAsync(joinCode);
		}
	}
}

