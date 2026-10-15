namespace LastProtocol
{
    public enum SessionState { Offline, Initializing, Creating, Joining, Connecting, Lobby, LoadingGame, InGame, Leaving, Error }
    public enum MatchPhase : byte { Lobby, Loading, Playing, Closing }
}
