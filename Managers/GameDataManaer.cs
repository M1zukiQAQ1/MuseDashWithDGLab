using System.CodeDom.Compiler;
using System.Collections.Generic;
using Il2CppAssets.Scripts.Database;
using Il2CppAssets.Scripts.GameCore.HostComponent;
using Il2CppAssets.Scripts.PeroTools.Commons;
using Il2CppFormulaBase;
using Il2CppGameLogic;

namespace MuseDashXDGLab {
    public static class GameDataHelper {
        public static int MissCount => Singleton<TaskStageTarget>.instance.GetMiss();
    }
}