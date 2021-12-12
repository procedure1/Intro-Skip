using IPA;
using SiraUtil.Zenject;
using IntroSkip.Installers;
using IntroSkip.UI;
using Conf = IPA.Config.Config;
using IPA.Logging;
using IPA.Config.Stores;
using SiraUtil.Attributes;

namespace IntroSkip
{
    [Plugin(RuntimeOptions.DynamicInit), Slog]
    internal class Plugin
    {
        [Init]
        public void Init(Conf conf, Logger logger, Zenjector zenjector)
        {
            Config config = conf.Generated<Config>();
            Utilities.MigrateConfig(ref config);

            zenjector.UseLogger(logger);
            zenjector.Install(Location.App, Container => Container.BindInstance(config).AsCached());
            zenjector.Install<IntroSkipGameInstaller>(Location.StandardPlayer | Location.CampaignPlayer);
            zenjector.Install(Location.Menu, Container => Container.BindInterfacesTo<ModifierUI>().AsSingle());
        }
    }
}