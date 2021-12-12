using SiraUtil.Logging;
using UnityEngine;
using UnityEngine.XR;
using Zenject;

namespace IntroSkip
{
    internal class SkipDaemon : IInitializable, ITickable
    {
        private readonly Config _config;
        private readonly SiraLog _siraLog;
        private readonly IVRPlatformHelper _vrPlatformHelper;
        private readonly IDifficultyBeatmap _difficultyBeatmap;
        private readonly ISkipDisplayService _skipDisplayService;
        private AudioTimeSyncController _audioTimeSyncController;
        private readonly AudioTimeSyncController.InitData _initData;
        private readonly VRControllersInputManager _vrControllersInputManager;

        private float _introSkipTime = -1f;
        private float _outroSkipTime = -1f;
        private bool _skippableOutro = false;
        private bool _skippableIntro = false;
        private float _lastObjectSkipTime = -1f;

        public bool CanSkip => InIntroPhase || InOutroPhase;
        public bool InIntroPhase => (Utilities.AudioTimeSyncSource(ref _audioTimeSyncController).time < _introSkipTime) && _skippableIntro;
        public bool InOutroPhase => Utilities.AudioTimeSyncSource(ref _audioTimeSyncController).time > _lastObjectSkipTime && Utilities.AudioTimeSyncSource(ref _audioTimeSyncController).time < _outroSkipTime && _skippableOutro;
        public bool WantsToSkip => _audioTimeSyncController.state == AudioTimeSyncController.State.Playing && (_vrControllersInputManager.TriggerValue(XRNode.LeftHand) >= .8 || _vrControllersInputManager.TriggerValue(XRNode.RightHand) >= .8 || Input.GetKey(KeyCode.I));

        public SkipDaemon(Config config, SiraLog siraLog, IVRPlatformHelper vrPlatformHelper, IDifficultyBeatmap difficultyBeatmap, ISkipDisplayService skipDisplayService, AudioTimeSyncController audioTimeSyncController, VRControllersInputManager vrControllersInputManager, AudioTimeSyncController.InitData initData)
        {
            _config = config;
            _siraLog = siraLog;
            _initData = initData;
            _vrPlatformHelper = vrPlatformHelper;
            _difficultyBeatmap = difficultyBeatmap;
            _skipDisplayService = skipDisplayService;
            _audioTimeSyncController = audioTimeSyncController;
            _vrControllersInputManager = vrControllersInputManager;
        }

        public void Initialize()
        {
            _skippableIntro = false;
            _skippableOutro = false;
            _introSkipTime = -1;
            _outroSkipTime = -1;
            _lastObjectSkipTime = -1;

            var lineData = _difficultyBeatmap.beatmapData.beatmapLinesData;
            float firstObjectTime = _initData.audioClip.length;
            float lastObjectTime = -1f;
            foreach (var line in lineData)
            {
                foreach (var beatmapObject in line.beatmapObjectsData)
                {
                    switch (beatmapObject.beatmapObjectType)
                    {
                        case BeatmapObjectType.Note:
                            if (beatmapObject.time < firstObjectTime)
                                firstObjectTime = beatmapObject.time;
                            if (beatmapObject.time > lastObjectTime)
                                lastObjectTime = beatmapObject.time;
                            break;
                        case BeatmapObjectType.Obstacle:
                            ObstacleData obstacle = (beatmapObject as ObstacleData)!;
                            if (!(obstacle.lineIndex == 0 && obstacle.width == 1) && !(obstacle.lineIndex == 3 && obstacle.width == 1))
                            {
                                if (beatmapObject.time < firstObjectTime)
                                    firstObjectTime = beatmapObject.time;
                                if (beatmapObject.time > lastObjectTime)
                                    lastObjectTime = beatmapObject.time;
                            }
                            break;
                    }
                }
            }
            if (firstObjectTime > 5f)
            {
                _skippableIntro = _config.AllowIntroSkip;
                _introSkipTime = firstObjectTime - 2f;
            }
            if ((_initData.audioClip.length - lastObjectTime) >= 5f)
            {
                _skippableOutro = _config.AllowOutroSkip;
                _outroSkipTime = _initData.audioClip.length - 1.5f;
                _lastObjectSkipTime = lastObjectTime + 0.5f;
            }
            _siraLog.Debug($"Skippable Intro: {_skippableIntro} | Skippable Outro: {_skippableOutro}");
            _siraLog.Debug($"First Object Time: {firstObjectTime} | Last Object Time: {lastObjectTime}");
            _siraLog.Debug($"Intro Skip Time: {_introSkipTime} | Outro Skip Time: {_outroSkipTime}");
        }

        public void Tick()
        {
            if (CanSkip)
            {
                if (!_skipDisplayService.Active)
                    _skipDisplayService.Show();

                if (WantsToSkip)
                {
                    _vrPlatformHelper.TriggerHapticPulse(XRNode.LeftHand, 0.1f, 0.2f, 1);
                    _vrPlatformHelper.TriggerHapticPulse(XRNode.RightHand, 0.1f, 0.2f, 1);
                    if (InIntroPhase)
                    {
                        Utilities.AudioTimeSyncSource(ref _audioTimeSyncController).time = _introSkipTime;
                        _skippableIntro = false;
                    }
                    else if (InOutroPhase)
                    {
                        Utilities.AudioTimeSyncSource(ref _audioTimeSyncController).time = _outroSkipTime;
                        _skippableOutro = false;
                    }
                }
            }
            else if (_skipDisplayService.Active && !CanSkip)
            {
                _skipDisplayService.Hide();
                return;
            }
        }
    }
}