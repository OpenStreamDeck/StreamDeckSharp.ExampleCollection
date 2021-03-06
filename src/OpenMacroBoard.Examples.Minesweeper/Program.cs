using OpenMacroBoard.Examples.CommonStuff;
using StreamDeckSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace OpenMacroBoard.Examples.Minesweeper
{
    internal class Program
    {
        private static void Main()
        {
            using var deck = ExampleHelper.OpenBoard();
            using var wrapper = new MinesweeperDeckWrapper(deck, new ClassicMinesweeperIconSet(), 300);
            ExampleHelper.WaitForKeyToExit();
        }
    }
}
