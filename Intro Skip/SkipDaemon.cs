using BeatSaberMarkupLanguage;
using SiraUtil.Logging;
using TMPro;
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
        private readonly AudioTimeSyncController _audioTimeSyncController;
        private readonly VRControllersInputManager _vrControllersInputManager;
        private readonly AudioTimeSyncController.InitData _initData;

        private bool _init = false;
        private bool _skippableOutro = false;
        private bool _skippableIntro = false;
        private float _introSkipTime = -1f;
        private float _outroSkipTime = -1f;
        private float _lastObjectSkipTime = -1f;
        private TextMeshProUGUI? _skipPrompt;

        public SkipDaemon(Config config, SiraLog siraLog, IVRPlatformHelper vrPlatformHelper, IDifficultyBeatmap difficultyBeatmap, AudioTimeSyncController audioTimeSyncController, VRControllersInputManager vrControllersInputManager, AudioTimeSyncController.InitData initData)
        {
            _config = config;
            _siraLog = siraLog;
            _initData = initData;
            _vrPlatformHelper = vrPlatformHelper;
            _difficultyBeatmap = difficultyBeatmap;
            _audioTimeSyncController = audioTimeSyncController;
            _vrControllersInputManager = vrControllersInputManager;
        }

        public void Initialize()
        {
            CreatePrompt();
            ReadMap();
        }

        public void ReInit()
        {
            _init = false;
            _skippableIntro = false;
            _skippableOutro = false;
            _introSkipTime = -1;
            _outroSkipTime = -1;
            _lastObjectSkipTime = -1;
            ReadMap();
        }

        public void ReadMap()
        {
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
            _init = true;
            _siraLog.Debug($"Skippable Intro: {_skippableIntro} | Skippable Outro: {_skippableOutro}");
            _siraLog.Debug($"First Object Time: {firstObjectTime} | Last Object Time: {lastObjectTime}");
            _siraLog.Debug($"Intro Skip Time: {_introSkipTime} | Outro Skip Time: {_outroSkipTime}");
        }

        private void CreatePrompt()
        {
            var skipPromptObject = new GameObject("IntroSkip Prompt");
            skipPromptObject.transform.position = new Vector3(-2.5f, 2.1f, 7.0f);
            skipPromptObject.transform.localScale = new Vector3(0.025f, 0.025f, 0.025f);

            Canvas _canvas = skipPromptObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.enabled = false;
            var rectTransform = _canvas.transform as RectTransform;
            rectTransform!.sizeDelta = new Vector2(100, 50);

            _skipPrompt = BeatSaberUI.CreateText(_canvas.transform as RectTransform, "Press Trigger To Skip", new Vector2(0, 10));
            rectTransform = _skipPrompt.transform as RectTransform;
            rectTransform!.SetParent(_canvas.transform, false);
            rectTransform.sizeDelta = new Vector2(100, 20);
            _skipPrompt.fontSize = 15f;
            _canvas.enabled = true;
            _skipPrompt.gameObject.SetActive(false);
        }

        public void Tick()
        {
            if (!_init || _skipPrompt == null)
                return;

            if (!(_skippableIntro || _skippableOutro))
            {
                if (_skipPrompt.gameObject.activeSelf)
                    _skipPrompt.gameObject.SetActive(false);
                return;
            }

            float time = _audioTimeSyncController.audioSource.time;
            bool introPhase = (time < _introSkipTime) && _skippableIntro;
            bool outroPhase = (time > _lastObjectSkipTime && time < _outroSkipTime) && _skippableOutro;

            if (introPhase || outroPhase)
            {
                if (!_skipPrompt.gameObject.activeSelf)
                    _skipPrompt.gameObject.SetActive(true);
            }
            else
            {
                if (_skipPrompt.gameObject.activeSelf)
                    _skipPrompt.gameObject.SetActive(false);
                return;
            }
            if (_audioTimeSyncController.state == AudioTimeSyncController.State.Playing && (_vrControllersInputManager.TriggerValue(UnityEngine.XR.XRNode.LeftHand) >= .8 || _vrControllersInputManager.TriggerValue(XRNode.RightHand) >= .8 || Input.GetKey(KeyCode.I)))
            {
                _siraLog.Debug("Skip Triggered At:" + time);
                _vrPlatformHelper.TriggerHapticPulse(UnityEngine.XR.XRNode.LeftHand, 0.1f, 0.2f, 1);
                _vrPlatformHelper.TriggerHapticPulse(UnityEngine.XR.XRNode.RightHand, 0.1f, 0.2f, 1);
                if (introPhase)
                {
                    _audioTimeSyncController.audioSource.time = _introSkipTime;
                    _skippableIntro = false;
                }
                else if (outroPhase)
                {
                    _audioTimeSyncController.audioSource.time = _outroSkipTime;
                    _skippableOutro = false;
                }
            }
        }
    }
}