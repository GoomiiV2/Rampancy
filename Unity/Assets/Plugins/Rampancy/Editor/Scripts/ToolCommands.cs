using System;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;
using Plugins.Rampancy.Runtime;

namespace Plugins.Rampancy.Editor.Scripts
{
    public static class ToolCommands
    {
        const int INPUT_WAIT_MS = 60;

        // Start tag test and inject input to get it to open the map
        public static void LaunchTagTestToMap(string mapPath)
        {
            var tagTestPs = Runtime.Rampancy.LaunchProgram(Runtime.Rampancy.Config.ActiveGameConfig.TagTestPath, "-windowed");
            Thread.Sleep(TimeSpan.FromSeconds(2));

            InputInjector.SetForegroundWindow(tagTestPs.MainWindowHandle);
            var sim = new InputSimulator();
            sim.Keyboard.KeyPress(VirtualKeyCode.OEM_3)
               .Sleep(INPUT_WAIT_MS); // Open the console

            sim.Keyboard.TextEntry($"framerate_throttle 1")
               .Sleep(INPUT_WAIT_MS)
               .KeyPress(VirtualKeyCode.RETURN)
               .Sleep(INPUT_WAIT_MS);

            sim.Keyboard.TextEntry($"map_name {mapPath}")
               .Sleep(INPUT_WAIT_MS)
               .KeyPress(VirtualKeyCode.RETURN)
               .Sleep(INPUT_WAIT_MS)
               .KeyPress(VirtualKeyCode.OEM_3);
        }
    }
}