namespace Numchen.Api.Features.Game;

public class GameHubOptions
{
    public TimeSpan PlacementTimeout { get; set; } = TimeSpan.FromSeconds(45);
    public TimeSpan FinishingTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan DisconnectGracePeriod { get; set; } = TimeSpan.FromSeconds(30);
}
