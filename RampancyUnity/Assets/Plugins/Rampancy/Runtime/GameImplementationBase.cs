using RampantC20;

namespace Rampancy
{
    // Common interface for game integrations to extend from
    public class GameImplementationBase
    {
        // To enable the menu options for these
        public virtual bool CanOpenSapien()
        {
            return true;
        }

        public virtual bool CanOpenGuerilla()
        {
            return true;
        }

        public virtual bool CanOpenTagTest()
        {
            return true;
        }

        public virtual bool CanCompileStructure()
        {
            return true;
        }

        public virtual bool CanCompileLightmaps()
        {
            return true;
        }

        public virtual bool CanCompileStructureAndLightmaps()
        {
            return true;
        }

        public virtual bool CanImportScene()
        {
            return true;
        }

        public virtual bool CanExportScene()
        {
            return true;
        }

        public virtual void OpenSapien()
        {
            Utils.RunExeIfExists(Rampancy.Cfg.ActiveGameConfig.SapienPath);
        }

        public virtual void OpenGuerilla()
        {
            Utils.RunExeIfExists(Rampancy.Cfg.ActiveGameConfig.GuerillaPath);
        }

        public virtual void OpenTagTest()
        {
            Utils.RunExeIfExists(Rampancy.Cfg.ActiveGameConfig.TagTestPath);
        }

        public virtual void OpenInTagTest(string mapPath = null)
        {
        }

        public virtual void CompileStructure()
        {
        }

        public virtual void CompileLightmaps(bool preview = true, float quality = 1.0f)
        {
        }

        public virtual void CompileStructureAndLightmaps()
        {
        }

        public virtual void ImportScene(string path = null)
        {
        }

        public virtual void ExportScene(string path = null)
        {
        }

        public virtual void ImportMaterial(string path)
        {
        }

        public virtual void SyncMaterials()
        {
        }
    }
}