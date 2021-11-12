using Zenject;

namespace IntroSkip.Installers
{
    internal class IntroSkipGameInstaller : Installer
    {
        private readonly Config _config;
        private readonly GameplayCoreSceneSetupData _gameplayCoreSceneSetupData;

        public IntroSkipGameInstaller(Config config, GameplayCoreSceneSetupData gameplayCoreSceneSetupData)
        {
            _config = config;
            _gameplayCoreSceneSetupData = gameplayCoreSceneSetupData;
        }

        public override void InstallBindings()
        {
            if ((_config.AllowIntroSkip || _config.AllowOutroSkip) && _gameplayCoreSceneSetupData.practiceSettings == null)
            {
                Container.BindInterfacesTo<SkipDaemon>().AsSingle();
            }
        }
    }
}