using System;
using System.IO;
using System.Linq;
using RampantC20;
using UnityEditor;
using UnityEngine;

namespace Rampancy.UI
{
    public class CreateLevelPopup : EditorWindow
    {
        public string       LevelName   = string.Empty;
        public bool         IsSP        = true;
        public GameVersions GameVersion = Rampancy.Cfg.GameVersion;
        public string       Location    = string.Empty;

        private int      LocationIdx     = 0;
        private string[] LocationOptions = {"Custom"};
        private bool     DoesLevelExist  = false;
        private string   LocationOption => LocationOptions[LocationIdx];
        private bool     ShowCreateNewFolderView = false;
        private string   TempNewDirName          = string.Empty;


        public CreateLevelPopup()
        {
            BuildLocationOptions();
            CheckIfLevelExists();
        }

        public static void Show()
        {
            var window = CreateInstance<CreateLevelPopup>();
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 300, 150);
            //window.ShowPopup();
            window.titleContent = new GUIContent("Create New Level");
            window.ShowModalUtility();
        }

        private void BuildLocationOptions()
        {
            try {
                var basePath  = Rampancy.Cfg.GetGameConfig(GameVersion).DataPath;
                var levelsDir = Path.Combine(basePath, "levels");

                var dirs = Directory.GetDirectories(levelsDir).Select(x => x[(levelsDir.Length + 1)..]);
                LocationOptions = dirs.Concat(new[] {"-- New Dir --", "-- Root --"}).Reverse().ToArray();
            }
            catch (Exception e) {
                LocationOptions = null;
            }
        }

        private string GetDataRelLevelLocation()
        {
            if (LocationIdx == 0) {
                var path = Path.Combine("levels", LevelName);
                return path;
            }
            else {
                var path = Path.Combine("levels", LocationOption, LevelName);
                return path;
            }
        }

        private void CheckIfLevelExists()
        {
            var basePath  = Rampancy.Cfg.GetGameConfig(GameVersion).DataPath;
            var levelsDir = Path.Combine(basePath, GetDataRelLevelLocation());
            DoesLevelExist = Directory.Exists(levelsDir);
        }

        private bool IsLevelNameValid()
        {
            var isValid = string.IsNullOrEmpty(LevelName) || string.IsNullOrWhiteSpace(LevelName);
            return isValid;
        }

        private void CreateNewLevelDir(string name)
        {
            if (string.IsNullOrEmpty(name)) return;

            var basePath  = Rampancy.Cfg.GetGameConfig(GameVersion).DataPath;
            var levelsDir = Path.Combine(basePath, "levels", name);
            Directory.CreateDirectory(levelsDir);
            
            Debug.Log($"Created a new level dir {name}, full path: {levelsDir}");
        }

        private void CreateLevel()
        {
            var gameImpl = Rampancy.GameImplementation[GameVersion];
            gameImpl.CreateNewScene(LevelName, Location, GameVersion);
            Debug.Log($"Created new level for {GameVersion} named {LevelName} at {Location}");

            Close();
        }

        private void OnGUI()
        {
            if (ShowCreateNewFolderView) {
                DrawCreateNewFolder();
            }
            else {
                DrawCreateNewLevel();
            }
        }

        private void DrawCreateNewFolder()
        {
            GUILayout.Label("Enter the name of the new folder:");
            TempNewDirName = GUILayout.TextField(TempNewDirName);

             // @formatter:off
            GUILayout.BeginHorizontal();
                if (UI.ButtonSuccess("Create")) {
                    if (!string.IsNullOrEmpty(TempNewDirName) && !string.IsNullOrWhiteSpace(TempNewDirName)) {
                        CreateNewLevelDir(TempNewDirName);
                        BuildLocationOptions();
                        LocationIdx = Array.FindIndex(LocationOptions, x => x == TempNewDirName);
                        TempNewDirName = string.Empty;
                    }
                    ShowCreateNewFolderView = false;
                }
                
                if (UI.ButtonDefault("Cancel")) {
                    ShowCreateNewFolderView = false;
                    LocationIdx = 0;
                }
            GUILayout.EndHorizontal();
            // @formatter:on 
        }

        private void DrawCreateNewLevel()
        {
            // @formatter:off
            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal();
                GUILayout.Label("Game:");
                GameVersion = (GameVersions)EditorGUILayout.EnumPopup("", GameVersion, GUILayout.Width(150));
            GUILayout.EndHorizontal();
            // @formatter:on 

            if (EditorGUI.EndChangeCheck()) {
                BuildLocationOptions();
                LocationIdx = 0;
            }

            if (LocationOptions == null) {
                DrawGameNotSetup();
                return;
            }

            EditorGUI.BeginChangeCheck();
            // @formatter:off
            GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(
                        "Level Root Location:",
                        "The folder that will contain your level.\n"+
                        "eg. Picking \"Test\" will result in your level being at \"levels/Test/YourLevel\"\n"+
                        "\"-- Root --\" will place it at \"levels/YourLevel\""));
                LocationIdx = EditorGUILayout.Popup(LocationIdx, LocationOptions, GUILayout.Width(150));
            GUILayout.EndHorizontal();
            // @formatter:on 

            // @formatter:off
            GUILayout.BeginHorizontal();
                GUILayout.Label("Level Name:");
                LevelName = EditorGUILayout.TextField(LevelName, GUILayout.Width(150));
            GUILayout.EndHorizontal();
            // @formatter:on 

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            if (EditorGUI.EndChangeCheck()) {
                if (LocationIdx == 1) {
                    ShowCreateNewFolderView = true;
                }

                Location = LocationOptions[LocationIdx];
                
                CheckIfLevelExists();
            }
            
            if (IsLevelNameValid()) {
                DrawPleaseSetLevelName();
                return;
            }

            if (DoesLevelExist) {
                DrawLevelWithThatNameExists();
                return;
            }

            var levelLoc = GetDataRelLevelLocation();
            GUILayout.Label($"Level will be created at: \n{levelLoc}");

            // @formatter:off
            GUILayout.BeginHorizontal();
                if (GUILayout.Button("Create")) CreateLevel();
                
                if (GUILayout.Button("Cancel")) Close();
            GUILayout.EndHorizontal();
            // @formatter:on 
        }

        private void DrawGameNotSetup()
        {
            GUILayout.Space(10);
            GUILayout.Label($"Game settings for {GameVersion} not setup yet\nPlease go to \"Rampancy > Settings\" and set the path\nfor the game there :>");
        }

        private void DrawLevelWithThatNameExists()
        {
            GUILayout.Space(10);
            GUILayout.Label($"A level with that name already exists\nPlease try a different name or location.");
        }
        
        private void DrawPleaseSetLevelName()
        {
            GUILayout.Space(10);
            GUILayout.Label($"Please set a level name");
        }
    }
}