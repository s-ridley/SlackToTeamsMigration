namespace SlackToTeams.Services {
    internal interface IMigrationService {
        public Task StartAsync();
        public void StopAsync();
    }
}
