﻿using OpenMacroBoard.SDK;
using StreamDeckSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace OpenMacroBoard.Examples.CommonStuff
{
    public static class ExampleHelper
    {
        private static readonly IUsbHidHardware[] virtualFallback = new[]
        {
            Hardware.StreamDeck,
            Hardware.StreamDeckMini,
            Hardware.StreamDeckXL
        };

        private static int ctrlCCnt = 0;

        static ExampleHelper()
        {
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                Interlocked.Increment(ref ctrlCCnt);
            };
        }

        public static int CtrlCCount => ctrlCCnt;
        public static bool CtrlCWasPressed => ctrlCCnt > 0;

        /// <summary>
        /// Searches for a real classic stream deck or creates a virtual one.
        /// All examples are designed for the 5x3 classic StreamDeck.
        /// </summary>
        /// <returns></returns>
        public static IMacroBoard OpenBoard()
            => OpenStreamDeckHardware();

        public static IMacroBoard OpenStreamDeck()
            => OpenStreamDeckHardware(Hardware.StreamDeck);

        public static IMacroBoard OpenStreamDeckMini()
            => OpenStreamDeckHardware(Hardware.StreamDeckMini);

        public static IMacroBoard OpenStreamDeckXL()
            => OpenStreamDeckHardware(Hardware.StreamDeckXL);

        public static IDeviceReferenceHandle SelectBoard(IEnumerable<IDeviceReferenceHandle> devices)
        {
            var devList = devices.ToList();
            devList.Sort((a, b) => string.Compare(a.ToString(), b.ToString()));

            if (devList.Count < 1)
            {
                return null;
            }

            if (devList.Count == 1)
            {
                return devList[0];
            }

            var selected = ConsoleSelect(devList);
            Console.Clear();

            return selected;
        }

        public static T ConsoleSelect<T>(IEnumerable<T> elements)
        {
            var list = elements.ToArray();
            var select = -1;

            for (var i = 0; i < list.Length; i++)
            {
                Console.WriteLine($"[{i}] {list[i]}");
            }

            Console.WriteLine();

            do
            {
                Console.Write("Select: ");
                var selection = Console.ReadLine();

                if (int.TryParse(selection, out var id))
                {
                    select = id;
                }
            }
            while (select < 0 || select >= list.Length);

            return list[select];
        }

        public static void WaitForKeyToExit()
        {
            Console.WriteLine("Please press any key (on PC keyboard) to exit this example.");
            Console.ReadKey();
        }

        private static VirtualDeviceHandle GetSimulatedDeviceForHardware(IHardware hardware)
            => new VirtualDeviceHandle(hardware.Keys, $"Virtual {hardware.DeviceName}");

        private static IMacroBoard OpenStreamDeckHardware(params IUsbHidHardware[] allowedHardware)
            => SelectBoard(GetRealAndSimulatedDevices(allowedHardware)).Open();

        private static IEnumerable<IDeviceReferenceHandle> GetRealAndSimulatedDevices(params IUsbHidHardware[] allowedHardware)
        {
            var virtualHardware = allowedHardware;

            if (virtualHardware == null || virtualHardware.Length < 1)
            {
                virtualHardware = virtualFallback;
            }

            return Enumerable.Empty<IDeviceReferenceHandle>()
                .Concat(GetStreamDecks(true, allowedHardware))
                .Concat(GetStreamDecks(false, allowedHardware))
                .Concat(virtualHardware.Select(GetSimulatedDeviceForHardware));
        }

        private static IEnumerable<IDeviceReferenceHandle> GetStreamDecks(bool cached, params IUsbHidHardware[] allowedHardware)
        {
            var postfix = cached ? "default / cached" : "synced / not cached";

            return StreamDeck
                        .EnumerateDevices(allowedHardware)
                        .Select(d =>
                        {
                            d.UseWriteCache = cached;
                            return new CustomNameDeviceHandle($"{d.DeviceName} ({postfix})", d);
                        })
                        .Cast<IDeviceReferenceHandle>();
        }
    }
}
