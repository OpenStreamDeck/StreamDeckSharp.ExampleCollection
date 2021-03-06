﻿using OpenMacroBoard.Examples.CommonStuff;
using OpenMacroBoard.SDK;
using System;
using System.Threading;

namespace OpenMacroBoard.Examples.MemoryGame
{
    internal class Program
    {
        private static readonly Random rnd = new Random();

        //positon of restart button
        private static readonly int restartKey = 7;

        private static int mode = 0;
        private static readonly int[] openCard = new int[2];

        private static KeyBitmap restartIcon;
        private static readonly KeyBitmap[] iconsActive = new KeyBitmap[7];
        private static readonly KeyBitmap[] iconsInactive = new KeyBitmap[7];

        //14 slots for memory (7x2 cards)
        private static readonly int[] gameState = new int[] { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6 };
        private static readonly bool[] cardVisible = new bool[14];

        private static void Main()
        {
            InitializeIconBitmaps();

            using (var s = ExampleHelper.OpenStreamDeck())
            {
                s.KeyStateChanged += StreamDeckKeyHandler;
                StartGame(s);

                ExampleHelper.WaitForKeyToExit();
            }
        }

        private static void StartGame(IMacroBoard deck)
        {
            //suffle memory cards
            openCard[0] = -1;
            openCard[1] = -1;
            mode = 0;
            SuffleArray(gameState, rnd);

            for (var i = 0; i < cardVisible.Length; i++)
            {
                cardVisible[i] = false;
            }

            //Clear all tiles (except restart key)
            for (var i = 0; i < deck.Keys.Count; i++)
            {
                if (i != restartKey)
                {
                    deck.ClearKey(i);
                }
            }

            //(Re-)Draw restart key image
            deck.SetKeyBitmap(restartKey, restartIcon);
        }

        private static void RefreshKeyIcon(IMacroBoard deck, int cardId)
        {
            var keyId = cardId >= restartKey ? cardId + 1 : cardId;

            if (cardVisible[cardId])
            {
                if ((openCard[0] == cardId || openCard[1] == cardId))
                {
                    deck.SetKeyBitmap(keyId, iconsInactive[gameState[cardId]]);
                }
                else
                {
                    deck.SetKeyBitmap(keyId, iconsActive[gameState[cardId]]);
                }
            }
            else
            {
                deck.SetKeyBitmap(keyId, KeyBitmap.Black);
            }
        }

        private static void InitializeIconBitmaps()
        {
            restartIcon = IconLoader.LoadIconByName("restart.png", true);
            for (var i = 0; i < iconsActive.Length; i++)
            {
                var name = $"card{i}.png";
                iconsActive[i] = IconLoader.LoadIconByName(name, true);
                iconsInactive[i] = IconLoader.LoadIconByName(name, false);
            }
        }

        private static void SuffleArray<T>(T[] array, Random rnd)
        {
            for (var i = 0; i < array.Length; i++)
            {
                var pick = rnd.Next(array.Length - i) + i;

                //Swap elements
                var tmp = array[i];
                array[i] = array[pick];
                array[pick] = tmp;
            }
        }


        private static Thread sleepThread;

        private static readonly AutoResetEvent threadSleeper = new AutoResetEvent(false);
        private static readonly object closeCardLock = new object();
        private static void CloseCards(IMacroBoard deck)
        {
            lock (closeCardLock)
            {
                if (mode != 2)
                {
                    return;
                }

                cardVisible[openCard[0]] = false;
                cardVisible[openCard[1]] = false;
                var c1 = openCard[0];
                var c2 = openCard[1];
                openCard[0] = -1;
                openCard[1] = -1;
                RefreshKeyIcon(deck, c1);
                RefreshKeyIcon(deck, c2);
                mode = 0;
            }
        }

        private static void StreamDeckKeyHandler(object sender, KeyEventArgs e)
        {
            if (!(sender is IMacroBoard deck))
            {
                return;
            }

            if (e.Key == restartKey && e.IsDown)
            {
                StartGame(deck);
                return;
            }

            if (e.IsDown)
            {
                if (mode == 2)
                {
                    threadSleeper.Set();
                    CloseCards(deck);
                }

                var cardId = e.Key < restartKey ? e.Key : e.Key - 1;
                if (mode == 0)
                {
                    if (!cardVisible[cardId])
                    {
                        mode = 1;
                        openCard[0] = cardId;
                        cardVisible[cardId] = true;
                        RefreshKeyIcon(deck, cardId);
                    }
                }
                else if (mode == 1)
                {
                    if (!cardVisible[cardId])
                    {
                        openCard[1] = cardId;
                        cardVisible[cardId] = true;
                        RefreshKeyIcon(deck, cardId);
                        if (gameState[openCard[0]] == gameState[openCard[1]])
                        {
                            mode = 0;
                            var c1 = openCard[0];
                            var c2 = openCard[1];
                            openCard[0] = -1;
                            openCard[1] = -1;
                            RefreshKeyIcon(deck, c1);
                            RefreshKeyIcon(deck, c2);
                        }
                        else
                        {
                            mode = 2;
                            sleepThread = new Thread(() =>
                            {
                                var timeout = threadSleeper.WaitOne(2000);
                                CloseCards(deck);
                            });
                            sleepThread.Start();
                        }
                    }
                }
            }
        }
    }
}
