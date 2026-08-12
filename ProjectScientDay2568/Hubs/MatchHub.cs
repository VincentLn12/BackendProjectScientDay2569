using Microsoft.AspNetCore.SignalR;

namespace ProjectScientDay2568.Hubs
{
    public class MatchHub : Hub
    {
        // broadcast message to all clients
        public async Task SendMatchUpdate(Guid competitionLevelID, string message)
        {
            await Clients.Group(competitionLevelID.ToString()).SendAsync("ReceiveMatchUpdate", message);
        }

        // join group by tournament
        public async Task JoinTournamentGroup(string competitionLevelID)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, competitionLevelID);
        }

      
    }
}
