using Il2CppAssets.Scripts.PeroTools.Commons;
using MelonLoader;
using MuseDashMirror;
using UnityEngine;

namespace MuseDashXDGLab
{
    public enum FireMode
    {
        GraduallyIncrease, FixedValue
    }

    internal class Main : MelonMod
    {
        // The method called when mod is loaded
        private int lastGreat = 0;
        private int lastMiss = 0;
        // private int lastPerfect = 0;

        private float _timer;
        private float _lastSetTime;

        public int Intensity
        {
            private set
            {
                if (value > _maxIntensity)
                {
                    _intensity = _maxIntensity;
                    return;
                }
                _intensity = value;
            }
            get { return _intensity; }
        }
        private int _intensity;
        private string[] _waveData;
        private FireMode _fireMode;

        private int _maxIntensity = 50;
        private int _greatIncrement = 2;
        private int _missIncrement = 5;
        private int _timeBetweenPunishment;
        private int _baseIntensity;
        private bool _channelAEnabled = true;
        private bool _channelBEnabled = false;

        private MelonPreferences_Category intensityCategory;
        private MelonPreferences_Category serverCategory;
        private Managers.NetworkManager networkManager;
        private Patch.IntensityIndicator intensityIndicator;
        private string _serverAddress;

        public delegate void FireEventHandler(FireData fireData);
        public static event FireEventHandler FireEvent;

        public override void OnInitializeMelon()
        {
            ReadPreference();

            Intensity = _baseIntensity;

            networkManager = new(_serverAddress);
            networkManager.Initialize();
            intensityIndicator = new();

            PatchEvents.PnlMenuPatch += (sender, e) =>
                MuseDashMirror.UIComponents.ToggleUtils.CreatePnlMenuToggle("Channel-A Toggle", "Channel A", _channelAEnabled, (value) => {_channelAEnabled = value;});
            PatchEvents.PnlMenuPatch += (sender, e) =>
                MuseDashMirror.UIComponents.ToggleUtils.CreatePnlMenuToggle("Channel-B Toggle", "Channel B", _channelBEnabled, (value) => {_channelBEnabled = value;});
            PatchEvents.GameStartPatch += ClearAfterStageCompleted;

            PatchEvents.GameStartPatch += (sender, e) => ReadPreference();
            PatchEvents.GameStartPatch += (sender, e) => intensityIndicator.CreateIndicators(Intensity);
            PatchEvents.GameStartPatch += (sender, e) => networkManager.ClearAB(1);
        }
        private void ClearAfterStageCompleted(object sender, GameStartEventArgs e)
        {
            _lastSetTime = 0;
            Intensity = _baseIntensity;
            networkManager.Fire(new(_intensity: 0, _waveData: "", _channel: 1));
            networkManager.Fire(new(_intensity: 0, _waveData: "", _channel: 2));
            ClearChannel();
            lastGreat = 0;
            lastMiss = 0;
        }

        private void ReadPreference()
        {
            intensityCategory = MelonPreferences.GetCategory("intensityCategory");
            serverCategory = MelonPreferences.GetCategory("serverCategory");

            if (intensityCategory == null)
            {
                intensityCategory = MelonPreferences.CreateCategory("intensityCategory");
                intensityCategory.CreateEntry<int>("maxIntensity", 50);
                intensityCategory.CreateEntry<int>("greatIncrement", 2);
                intensityCategory.CreateEntry<int>("missIncrement", 5);
                intensityCategory.CreateEntry<int>("timeBetweenPunishment", 3);
                intensityCategory.CreateEntry<FireMode>("fireMode", FireMode.GraduallyIncrease);
                intensityCategory.CreateEntry<int>("baseIntensity", 10);

                intensityCategory.CreateEntry<string[]>("waveData", [
                    """["0A0A0A0A00000000","0A0A0A0A0A0A0A0A","0A0A0A0A14141414","0A0A0A0A1E1E1E1E","0A0A0A0A28282828","0A0A0A0A32323232","0A0A0A0A3C3C3C3C","0A0A0A0A46464646","0A0A0A0A50505050","0A0A0A0A5A5A5A5A","0A0A0A0A64646464"]""",
                    """["0A0A0A0A00000000","0D0D0D0D0F0F0F0F","101010101E1E1E1E","1313131332323232","1616161641414141","1A1A1A1A50505050","1D1D1D1D64646464","202020205A5A5A5A","2323232350505050","262626264B4B4B4B","2A2A2A2A41414141"]""",
                    """["4A4A4A4A64646464","4545454564646464","4040404064646464","3B3B3sB3B64646464","3636363664646464","3232323264646464","2D2D2D2D64646464","2828282864646464","2323232364646464","1E1E1E1E64646464","1A1A1A1A64646464"]"""
                ]);
            }
            if(serverCategory == null) {
                serverCategory = MelonPreferences.CreateCategory("serverCategory");
                serverCategory.CreateEntry<string>("ip", "ws://192.168.110.218:9999/");
            }

            _maxIntensity = intensityCategory.GetEntry<int>("maxIntensity").Value;
            _greatIncrement = intensityCategory.GetEntry<int>("greatIncrement").Value;
            _missIncrement = intensityCategory.GetEntry<int>("missIncrement").Value;
            _timeBetweenPunishment = intensityCategory.GetEntry<int>("timeBetweenPunishment").Value;
            _waveData = intensityCategory.GetEntry<string[]>("waveData").Value;
            _fireMode = intensityCategory.GetEntry<FireMode>("fireMode").Value;
            _baseIntensity = intensityCategory.GetEntry<int>("baseIntensity").Value;
            _serverAddress = serverCategory.GetEntry<string>("ip").Value;

            LoggerInstance.Msg("Remember to set max intensity to a SAFE range");
            LoggerInstance.Msg("Current maximum intensity is " + _maxIntensity);
        }

        // The method called when mod is unloaded
        public override void OnDeinitializeMelon()
        {

        }

        public override void OnUpdate()
        {
            _timer += Time.deltaTime;

            var currentGreatCount = BattleComponent.GreatCount;
            var currentMissCount = GameDataHelper.MissCount;

            if (_lastSetTime - _timer > _timeBetweenPunishment) return;

            if (lastGreat < currentGreatCount)
            {
                if (_fireMode == FireMode.GraduallyIncrease)
                {
                    Intensity += _greatIncrement;
                }

                lastGreat = currentGreatCount;
                Fire();
            }

            if (lastMiss < currentMissCount)
            {
                if (_fireMode == FireMode.GraduallyIncrease)
                {
                    Intensity += _missIncrement;
                }

                lastMiss = currentMissCount;
                Fire();
            }

            _lastSetTime = _timer;
        }

        private void Fire() {
            var nextRandomNumber = UnityEngine.Random.Range(0, _waveData.Length);
            if (_channelAEnabled) {
                FireEvent.Invoke(new FireData(_intensity: Intensity,
                    _waveData: _waveData[nextRandomNumber],
                    _channel: 1));
            }
            if(_channelBEnabled) {
                FireEvent.Invoke(new FireData(_intensity: Intensity,
                    _waveData: _waveData[nextRandomNumber],
                    _channel: 2));
            }
        }

        private void ClearChannel() {
            if (_channelAEnabled) networkManager.ClearAB(1);
            if (_channelBEnabled) networkManager.ClearAB(2);
        }

        private void ClearChannel(int channel) {
            networkManager.ClearAB(channel);
        }
    }

    public class FireData {
        public int strength;
        public string waveData;
        public int channel;

        public FireData(int _intensity, string _waveData, int _channel) {
            strength = _intensity;
            waveData = _waveData;
            channel = _channel;
        }
    }
}